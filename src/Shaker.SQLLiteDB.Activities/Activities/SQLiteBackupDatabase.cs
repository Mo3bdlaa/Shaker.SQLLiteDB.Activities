using System.Activities;
using System.ComponentModel;
using System.IO;
using Microsoft.Data.Sqlite;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Maintenance
{
    /// <summary>
    /// Copies a live database to another file with the SQLite online backup API. Unlike copying the
    /// file by hand this is safe while other robots are reading and writing. Encrypted databases are
    /// copied with SQLCipher's own export, and the copy can be given a different password.
    /// </summary>
    [Category("SQLite.Maintenance")]
    [DisplayName("SQLite Backup Database")]
    [Description("Creates a consistent copy of the database with the online backup API, safe to run while the database is in use.")]
#if NET6_0_OR_GREATER
    [System.Activities.ViewModels.ViewModelClass(typeof(Shaker.SQLLiteDB.Activities.Design.SQLiteBackupDatabaseViewModel))]
#endif
    public class SQLiteBackupDatabase : SQLiteActivityBase<string>
    {
        public SQLiteBackupDatabase()
        {
            DisplayName = "SQLite Backup Database";
        }

        [Category("Input")]
        [DisplayName("Destination path")]
        [Description("Where the copy is written. An existing file is replaced.")]
        [RequiredArgument]
        public InArgument<string> DestinationPath { get; set; }

        [Category("Options")]
        [DisplayName("Backup password")]
        [Description("Password of the copy. Empty keeps the password of the source, so an encrypted database is backed up encrypted.")]
        public InArgument<string> BackupPassword { get; set; }

        [Category("Options")]
        [DisplayName("Overwrite")]
        [Description("Replace the destination file when it already exists.")]
        public bool Overwrite { get; set; } = true;

        [Category("Output")]
        [DisplayName("Size (bytes)")]
        [Description("Size of the backup file that was written.")]
        public OutArgument<long> SizeInBytes { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var destination = DestinationPath.Get(context);
            var overwrite = Overwrite;
            var backupPassword = GetValue(BackupPassword, context, null);
            var size = new long[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    var fullPath = Path.GetFullPath(destination);

                    if (File.Exists(fullPath))
                    {
                        if (!overwrite)
                        {
                            throw new SQLiteActivityException("The backup file already exists and 'Overwrite' is off: " + fullPath);
                        }

                        File.Delete(fullPath);
                    }

                    var folder = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    using (var lease = request.Acquire())
                    {
                        var sourcePassword = lease.Handle.Settings.Password;

                        if (!string.IsNullOrEmpty(sourcePassword) || !string.IsNullOrEmpty(backupPassword))
                        {
                            // The ADO.NET online backup API refuses encrypted databases, and it cannot
                            // set a password on the copy either, so SQLCipher's own export is used.
                            var source = lease.Handle.Settings.Clone();
                            SQLiteEncryption.ExportTo(source, fullPath,
                                string.IsNullOrEmpty(backupPassword) ? sourcePassword : backupPassword,
                                cancellationToken);
                        }
                        else
                        {
                            lease.Handle.Execute((connection, transaction) =>
                            {
                                using (var target = new SqliteConnection("Data Source=" + fullPath + ";Mode=ReadWriteCreate"))
                                {
                                    target.Open();
                                    connection.BackupDatabase(target);
                                }

                                return 0;
                            }, cancellationToken);
                        }
                    }

                    size[0] = new FileInfo(fullPath).Length;
                    return fullPath;
                },
                ApplyOutputs = (activityContext, result) => SetValue(SizeInBytes, activityContext, size[0])
            };
        }
    }
}
