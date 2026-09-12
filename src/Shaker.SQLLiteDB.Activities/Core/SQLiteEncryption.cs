using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>What a password change actually did.</summary>
    public enum SQLitePasswordOperation
    {
        /// <summary>The database already was in the requested state.</summary>
        Unchanged = 0,
        /// <summary>A plain database was turned into an encrypted one.</summary>
        Encrypted = 1,
        /// <summary>The password of an encrypted database was replaced.</summary>
        PasswordChanged = 2,
        /// <summary>An encrypted database was turned back into a plain one.</summary>
        Decrypted = 3
    }

    /// <summary>Outcome of a password change.</summary>
    public class SQLitePasswordChangeResult
    {
        /// <summary>Which operation was carried out.</summary>
        public SQLitePasswordOperation Operation { get; set; }

        /// <summary>The database that was worked on.</summary>
        public string DatabasePath { get; set; }

        /// <summary>Copy of the database as it was before the change, or null when none was kept.</summary>
        public string BackupPath { get; set; }

        public override string ToString()
        {
            return Operation.ToString();
        }
    }

    /// <summary>
    /// Encrypts, decrypts and re-keys a database with SQLCipher.
    /// <para>
    /// Changing the password of an already encrypted database is done in place with <c>PRAGMA rekey</c>.
    /// Encrypting a plain database, or decrypting an encrypted one, cannot be done in place: SQLCipher
    /// writes a new file with <c>sqlcipher_export</c>, which this class then swaps in, keeping the
    /// previous file as a backup.
    /// </para>
    /// </summary>
    public static class SQLiteEncryption
    {
        private const int SqliteNotADatabase = 26;

        /// <summary>
        /// True when the file cannot be read without a password. A file that does not exist, or that is
        /// readable as-is, counts as not encrypted.
        /// </summary>
        public static bool IsEncrypted(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath) || !File.Exists(databasePath))
            {
                return false;
            }

            SQLiteNative.EnsureInitialized();

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            };

            try
            {
                using (var connection = new SqliteConnection(builder.ToString()))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "select count(*) from sqlite_master;";
                        command.ExecuteScalar();
                    }
                }

                return false;
            }
            catch (SqliteException ex) when ((ex.SqliteErrorCode & 0xFF) == SqliteNotADatabase)
            {
                // "file is not a database" is what SQLCipher reports for an encrypted file opened
                // without the right key. A genuinely corrupt file looks the same from here.
                return true;
            }
        }

        /// <summary>
        /// Copies an encrypted database to another file with <c>sqlcipher_export</c>. The ADO.NET online
        /// backup API refuses to touch encrypted databases, so this is how their backups are made.
        /// </summary>
        /// <param name="settings">Connection settings of the source, including its password.</param>
        /// <param name="destinationPath">File to write. It must not exist yet.</param>
        /// <param name="destinationPassword">Password of the copy. Empty writes a plain, unencrypted copy.</param>
        /// <param name="cancellationToken">Cancels the operation.</param>
        public static void ExportTo(SQLiteConnectionSettings settings, string destinationPath, string destinationPassword, CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            // ATTACH inherits the open flags of the connection, so the copy can only be created when the
            // connection itself may create files.
            var working = settings.Clone();
            working.Pooling = false;
            working.OpenMode = SQLiteOpenMode.ReadWriteCreate;
            working.JournalMode = SQLiteJournalMode.Unchanged;

            using (var connection = working.Open())
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "attach database @target as shaker_backup key " + QuoteLiteral(destinationPassword ?? string.Empty) + ";" +
                        "select sqlcipher_export('shaker_backup');" +
                        "detach database shaker_backup;";
                    command.Parameters.AddWithValue("@target", destinationPath);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Sets, changes or removes the password of a database. Pass an empty
        /// <paramref name="newPassword"/> to decrypt it.
        /// </summary>
        /// <param name="settings">Connection settings, including the <b>current</b> password.</param>
        /// <param name="newPassword">The new password, or empty to remove the encryption.</param>
        /// <param name="keepBackup">Keep the previous file as <c>&lt;database&gt;.bak</c> when the file has to be rewritten.</param>
        /// <param name="cancellationToken">Cancels the operation.</param>
        public static SQLitePasswordChangeResult SetPassword(
            SQLiteConnectionSettings settings,
            string newPassword,
            bool keepBackup,
            CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (!SQLiteNative.IsEncryptionSupported)
            {
                throw new SQLiteActivityException(
                    "The loaded SQLite engine has no encryption support, so the password cannot be changed. " +
                    "This package ships a SQLCipher build; another package in the project has probably replaced it " +
                    "with a plain e_sqlite3 engine.");
            }

            var databasePath = settings.ResolvedDatabasePath;

            if (string.IsNullOrEmpty(databasePath))
            {
                throw new SQLiteActivityException("Only a database that lives in a file can be encrypted.");
            }

            if (!File.Exists(databasePath))
            {
                throw new SQLiteActivityException("The database does not exist: " + databasePath);
            }

            var currentPassword = settings.Password ?? string.Empty;
            newPassword = newPassword ?? string.Empty;

            var result = new SQLitePasswordChangeResult { DatabasePath = databasePath };

            if (currentPassword.Length == 0 && newPassword.Length == 0)
            {
                result.Operation = SQLitePasswordOperation.Unchanged;
                return result;
            }

            // Own connection, pooling off: the file handle has to be really gone before the file is
            // swapped. ReadWriteCreate matters as well, because ATTACH inherits the open flags of the
            // connection and the exported copy is a file that does not exist yet.
            var working = settings.Clone();
            working.Pooling = false;
            working.OpenMode = SQLiteOpenMode.ReadWriteCreate;
            working.JournalMode = SQLiteJournalMode.Unchanged;

            if (currentPassword.Length > 0 && newPassword.Length > 0)
            {
                Rekey(working, newPassword, cancellationToken);
                result.Operation = SQLitePasswordOperation.PasswordChanged;
                return result;
            }

            var backupPath = Export(working, databasePath, newPassword, keepBackup, cancellationToken);

            result.Operation = newPassword.Length > 0
                ? SQLitePasswordOperation.Encrypted
                : SQLitePasswordOperation.Decrypted;
            result.BackupPath = backupPath;
            return result;
        }

        private static void Rekey(SQLiteConnectionSettings settings, string newPassword, CancellationToken cancellationToken)
        {
            using (var connection = settings.Open())
            using (var command = connection.CreateCommand())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // PRAGMA rekey takes no parameters, so the value is quoted as a SQL string literal.
                command.CommandText = "pragma rekey = " + QuoteLiteral(newPassword) + ";";
                command.ExecuteNonQuery();
            }
        }

        private static string Export(SQLiteConnectionSettings settings, string databasePath, string newPassword, bool keepBackup, CancellationToken cancellationToken)
        {
            var temporaryPath = databasePath + ".converting.tmp";
            var backupPath = databasePath + ".bak";

            DeleteDatabaseFiles(temporaryPath);

            try
            {
                using (var connection = settings.Open())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var checkpoint = connection.CreateCommand())
                    {
                        // Fold the write ahead log in first, so the exported copy is complete.
                        checkpoint.CommandText = "pragma wal_checkpoint(TRUNCATE);";

                        try
                        {
                            checkpoint.ExecuteNonQuery();
                        }
                        catch (SqliteException)
                        {
                            // Not a WAL database: nothing to fold in.
                        }
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            "attach database @target as shaker_converted key " + QuoteLiteral(newPassword) + ";" +
                            "select sqlcipher_export('shaker_converted');" +
                            "detach database shaker_converted;";
                        command.Parameters.AddWithValue("@target", temporaryPath);
                        command.ExecuteNonQuery();
                    }
                }

                if (!File.Exists(temporaryPath))
                {
                    throw new SQLiteActivityException(
                        "SQLCipher did not produce the converted database. The original file was left untouched.");
                }

                // The connection is closed, so the journal side car files of the original are stale now.
                DeleteSideCarFiles(databasePath);

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Replace(temporaryPath, databasePath, backupPath);

                if (!keepBackup)
                {
                    TryDelete(backupPath);
                    return null;
                }

                return backupPath;
            }
            catch (SqliteException ex)
            {
                DeleteDatabaseFiles(temporaryPath);

                throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                    "The password of '{0}' could not be changed: {1}. The original database was left untouched.",
                    databasePath, ex.Message), ex);
            }
            catch
            {
                DeleteDatabaseFiles(temporaryPath);
                throw;
            }
        }

        /// <summary>Quotes a value as a SQL string literal, for the pragmas that take no parameters.</summary>
        private static string QuoteLiteral(string value)
        {
            return "'" + (value ?? string.Empty).Replace("'", "''") + "'";
        }

        private static void DeleteDatabaseFiles(string path)
        {
            TryDelete(path);
            DeleteSideCarFiles(path);
        }

        private static void DeleteSideCarFiles(string path)
        {
            TryDelete(path + "-wal");
            TryDelete(path + "-shm");
            TryDelete(path + "-journal");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // Leftovers are harmless; a locked file surfaces as a clear error further down.
            }
        }
    }
}
