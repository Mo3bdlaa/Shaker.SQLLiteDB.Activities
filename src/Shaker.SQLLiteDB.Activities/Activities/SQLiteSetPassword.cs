using System.Activities;
using System.ComponentModel;
using System.Globalization;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Maintenance
{
    /// <summary>
    /// Encrypts a database, changes the password of an encrypted one, or removes the encryption again.
    /// The engine that ships with this package is a SQLCipher build, so no extra install is needed.
    /// <para>
    /// Changing a password is done in place. Encrypting or decrypting rewrites the file, so the
    /// database must not be in use: put this activity outside a SQLite Connect Scope.
    /// </para>
    /// </summary>
    [Category("SQLite.Maintenance")]
    [DisplayName("SQLite Set Password")]
    [Description("Encrypts a database with AES-256 (SQLCipher), changes its password, or removes the encryption.")]
#if NET6_0_OR_GREATER
    [System.Activities.ViewModels.ViewModelClass(typeof(Shaker.SQLLiteDB.Activities.Design.SQLiteSetPasswordViewModel))]
#endif
    public class SQLiteSetPassword : SQLiteActivityBase<string>
    {
        /// <summary>Full path of the database the new password was applied to.</summary>
        [Category("Output")]
        [DisplayName("Database path")]
        [Description("Full path of the database the new password was applied to.")]
        public new OutArgument<string> Result
        {
            get { return base.Result; }
            set { base.Result = value; }
        }

        public SQLiteSetPassword()
        {
            DisplayName = "SQLite Set Password";
        }

        [Category("Input")]
        [DisplayName("New password")]
        [Description("The password to set. Leave it empty to decrypt the database and turn it back into a normal SQLite file.")]
        public InArgument<string> NewPassword { get; set; }

        [Category("Options")]
        [DisplayName("Keep backup")]
        [Description("When the file has to be rewritten (encrypting or decrypting), keep the previous version as '<database>.bak'.")]
        public bool KeepBackup { get; set; } = true;

        [Category("Output")]
        [DisplayName("Backup path")]
        [Description("Where the previous version of the database was kept, or empty when the change was made in place.")]
        public OutArgument<string> BackupPath { get; set; }

        [Category("Output")]
        [DisplayName("Was encrypted")]
        [Description("True when the database was already encrypted before this activity ran.")]
        public OutArgument<bool> WasEncrypted { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var existing = GetValue(Connection, context, null) ?? SQLiteExecutionProperties.FindConnection(context);
            var newPassword = GetValue(NewPassword, context, null) ?? string.Empty;
            var currentPassword = GetValue(Password, context, null) ?? string.Empty;
            var keepBackup = KeepBackup;

            // Encrypting and decrypting replace the database file, which cannot be done while a scope
            // holds it open. Changing the password of an already encrypted database is done in place and
            // would be safe, but the same rule is applied to both so the behaviour is easy to predict.
            if (existing != null)
            {
                throw new SQLiteActivityException(
                    "SQLite Set Password rewrites the database file, so it cannot run inside a SQLite Connect Scope. " +
                    "Move it outside the scope and give it a 'Database path'.");
            }

            var settings = BuildSettings(context);
            settings.Password = currentPassword;

            var outcome = new SQLitePasswordChangeResult[1];
            var wasEncrypted = new bool[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    var databasePath = settings.ResolvedDatabasePath;
                    wasEncrypted[0] = SQLiteEncryption.IsEncrypted(databasePath);

                    if (wasEncrypted[0] && currentPassword.Length == 0)
                    {
                        throw new SQLiteActivityException(
                            "This database is encrypted, so its current password must be supplied in 'Password' before it can be changed.");
                    }

                    // Writing a new database file is a write: queue behind any other writer.
                    var lockOptions = settings.Lock ?? new SQLiteLockOptions();

                    using (SQLiteWriteLock.Acquire(settings.DatabaseKey, lockOptions.ResolveLockFilePath(databasePath), lockOptions, cancellationToken))
                    {
                        var result = SQLiteEncryption.SetPassword(settings, newPassword, keepBackup, cancellationToken);
                        outcome[0] = result;

                        switch (result.Operation)
                        {
                            case SQLitePasswordOperation.Encrypted:
                                return string.Format(CultureInfo.InvariantCulture, "Encrypted '{0}'.", databasePath);
                            case SQLitePasswordOperation.Decrypted:
                                return string.Format(CultureInfo.InvariantCulture, "Removed the encryption from '{0}'.", databasePath);
                            case SQLitePasswordOperation.PasswordChanged:
                                return string.Format(CultureInfo.InvariantCulture, "Changed the password of '{0}'.", databasePath);
                            default:
                                return string.Format(CultureInfo.InvariantCulture, "'{0}' was already in the requested state.", databasePath);
                        }
                    }
                },
                ApplyOutputs = (activityContext, message) =>
                {
                    SetValue(WasEncrypted, activityContext, wasEncrypted[0]);
                    SetValue(BackupPath, activityContext, outcome[0] == null ? null : outcome[0].BackupPath);
                }
            };
        }
    }
}
