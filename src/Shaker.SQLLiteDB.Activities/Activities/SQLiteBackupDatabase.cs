using System.Activities;
using System.ComponentModel;
using System.IO;
using Microsoft.Data.Sqlite;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>
    /// Copies a live database to another file with the SQLite online backup API. Unlike copying the
    /// file by hand this is safe while other robots are reading and writing.
    /// </summary>
    [DisplayName("SQLite Backup Database")]
    [Description("Creates a consistent copy of the database with the online backup API, safe to run while the database is in use.")]
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

                    size[0] = new FileInfo(fullPath).Length;
                    return fullPath;
                },
                ApplyOutputs = (activityContext, result) => SetValue(SizeInBytes, activityContext, size[0])
            };
        }
    }
}
