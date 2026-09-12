using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>
    /// Everything needed to open a SQLite database: the file, the open mode and the PRAGMA tuning
    /// applied to every new connection.
    /// </summary>
    public class SQLiteConnectionSettings
    {
        /// <summary>Full or relative path of the database file. Ignored when <see cref="ConnectionString"/> is set.</summary>
        public string DatabasePath { get; set; }

        /// <summary>A ready made ADO.NET connection string. When set it wins over <see cref="DatabasePath"/>.</summary>
        public string ConnectionString { get; set; }

        /// <summary>How the file is opened. Default is <see cref="SQLiteOpenMode.ReadWriteCreate"/>.</summary>
        public SQLiteOpenMode OpenMode { get; set; } = SQLiteOpenMode.ReadWriteCreate;

        /// <summary>
        /// Password of an encrypted (SQLCipher) database. Leave it empty for a normal, unencrypted
        /// database. Setting it on a database that was created without one fails to open: use the
        /// <c>SQLite Set Password</c> activity to encrypt an existing database.
        /// </summary>
        public string Password { get; set; }

        /// <summary>Journal mode applied on open. Default is WAL, which is what makes concurrent readers possible.</summary>
        public SQLiteJournalMode JournalMode { get; set; } = SQLiteJournalMode.Wal;

        /// <summary>Durability level applied on open.</summary>
        public SQLiteSynchronousMode Synchronous { get; set; } = SQLiteSynchronousMode.Normal;

        /// <summary>Where temporary tables and indexes live.</summary>
        public SQLiteTempStore TempStore { get; set; } = SQLiteTempStore.Unchanged;

        /// <summary>How long the engine waits for a lock held by another connection before it returns SQLITE_BUSY.</summary>
        public int BusyTimeoutMilliseconds { get; set; } = 30000;

        /// <summary>Default command timeout in seconds.</summary>
        public int CommandTimeoutSeconds { get; set; } = 60;

        /// <summary>Enforce foreign key constraints. SQLite has them off by default; this library turns them on.</summary>
        public bool EnforceForeignKeys { get; set; } = true;

        /// <summary>Page cache size in kilobytes. Zero keeps the engine default.</summary>
        public int CacheSizeKilobytes { get; set; }

        /// <summary>Use the ADO.NET connection pool.</summary>
        public bool Pooling { get; set; } = true;

        /// <summary>Create the folder of <see cref="DatabasePath"/> when it does not exist yet.</summary>
        public bool CreateDirectoryIfMissing { get; set; } = true;

        /// <summary>Extra PRAGMA statements applied on every connection, for example <c>{"mmap_size", "268435456"}</c>.</summary>
        public IDictionary<string, string> ExtraPragmas { get; set; }

        /// <summary>Writer lock configuration used by every write operation on this database.</summary>
        public SQLiteLockOptions Lock { get; set; } = new SQLiteLockOptions();

        /// <summary>Retry configuration applied when SQLite reports "database is locked" / "database is busy".</summary>
        public SQLiteRetryOptions Retry { get; set; } = new SQLiteRetryOptions();

        /// <summary>True when the database is opened read only, and therefore never takes the writer lock.</summary>
        public bool IsReadOnly
        {
            get { return OpenMode == SQLiteOpenMode.ReadOnly; }
        }

        /// <summary>True for a pure in-memory database.</summary>
        public bool IsInMemory
        {
            get
            {
                if (OpenMode == SQLiteOpenMode.Memory)
                {
                    return true;
                }

                var source = ConnectionString != null ? TryGetDataSource(ConnectionString) : DatabasePath;
                return !string.IsNullOrEmpty(source) &&
                       source.IndexOf(":memory:", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        /// <summary>
        /// Stable identity of the target database, used as the key of the writer lock and of the in-process
        /// connection registry.
        /// </summary>
        public string DatabaseKey
        {
            get
            {
                var source = ConnectionString != null ? TryGetDataSource(ConnectionString) : DatabasePath;
                if (string.IsNullOrEmpty(source))
                {
                    return "sqlite::unknown";
                }

                if (source.IndexOf(":memory:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "sqlite::memory:" + source;
                }

                try
                {
                    return Path.GetFullPath(source).ToUpperInvariant();
                }
                catch
                {
                    return source.ToUpperInvariant();
                }
            }
        }

        /// <summary>Resolved full path of the database file, or null for an in-memory database.</summary>
        public string ResolvedDatabasePath
        {
            get
            {
                if (IsInMemory)
                {
                    return null;
                }

                var source = ConnectionString != null ? TryGetDataSource(ConnectionString) : DatabasePath;
                if (string.IsNullOrEmpty(source))
                {
                    return null;
                }

                try
                {
                    return Path.GetFullPath(source);
                }
                catch
                {
                    return source;
                }
            }
        }

        /// <summary>Builds the ADO.NET connection string for these settings.</summary>
        public string BuildConnectionString()
        {
            if (!string.IsNullOrWhiteSpace(ConnectionString))
            {
                return ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(DatabasePath))
            {
                throw new SQLiteActivityException(
                    "No database was specified. Set either 'DatabasePath' or 'ConnectionString', or run the activity inside a SQLite Connect Scope.");
            }

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = OpenMode == SQLiteOpenMode.Memory ? ":memory:" : DatabasePath,
                ForeignKeys = EnforceForeignKeys,
                DefaultTimeout = CommandTimeoutSeconds <= 0 ? 30 : CommandTimeoutSeconds
            };

            switch (OpenMode)
            {
                case SQLiteOpenMode.ReadWriteCreate:
                    builder.Mode = SqliteOpenMode.ReadWriteCreate;
                    break;
                case SQLiteOpenMode.ReadWrite:
                    builder.Mode = SqliteOpenMode.ReadWrite;
                    break;
                case SQLiteOpenMode.ReadOnly:
                    builder.Mode = SqliteOpenMode.ReadOnly;
                    break;
                case SQLiteOpenMode.Memory:
                    builder.Mode = SqliteOpenMode.Memory;
                    builder.Cache = SqliteCacheMode.Shared;
                    break;
            }

            if (!string.IsNullOrEmpty(Password))
            {
                builder.Password = Password;
            }

            builder.Pooling = Pooling;

            return builder.ToString();
        }

        /// <summary>Opens a new connection and applies the configured PRAGMA settings to it.</summary>
        public SqliteConnection Open()
        {
            SQLiteNative.EnsureInitialized();

            if (CreateDirectoryIfMissing && !IsReadOnly && !IsInMemory)
            {
                var path = ResolvedDatabasePath;
                if (!string.IsNullOrEmpty(path))
                {
                    var folder = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                }
            }

            var connection = new SqliteConnection(BuildConnectionString());
            try
            {
                connection.Open();
                ApplyPragmas(connection);
                Verify(connection);
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Reads one page of the schema so that a wrong or missing password fails here, where the error
        /// can be explained, instead of surfacing later as "file is not a database" on the first query.
        /// Opening an encrypted database without its password succeeds as far as SQLite is concerned.
        /// </summary>
        private static void Verify(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select count(*) from sqlite_master;";
                command.ExecuteScalar();
            }
        }

        private void ApplyPragmas(SqliteConnection connection)
        {
            var statements = new List<string>();

            if (BusyTimeoutMilliseconds > 0)
            {
                statements.Add("PRAGMA busy_timeout=" + BusyTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture) + ";");
            }

            // WAL is persistent and cannot be set on a read only or in-memory database.
            if (JournalMode != SQLiteJournalMode.Unchanged && !IsReadOnly && !IsInMemory)
            {
                statements.Add("PRAGMA journal_mode=" + JournalMode.ToString().ToUpperInvariant() + ";");
            }

            if (Synchronous != SQLiteSynchronousMode.Unchanged && !IsReadOnly)
            {
                statements.Add("PRAGMA synchronous=" + Synchronous.ToString().ToUpperInvariant() + ";");
            }

            if (TempStore != SQLiteTempStore.Unchanged)
            {
                statements.Add("PRAGMA temp_store=" + TempStore.ToString().ToUpperInvariant() + ";");
            }

            if (CacheSizeKilobytes > 0)
            {
                // A negative cache_size value means "kibibytes" instead of "pages".
                statements.Add("PRAGMA cache_size=-" + CacheSizeKilobytes.ToString(CultureInfo.InvariantCulture) + ";");
            }

            if (ExtraPragmas != null)
            {
                foreach (var pragma in ExtraPragmas)
                {
                    if (!string.IsNullOrWhiteSpace(pragma.Key))
                    {
                        statements.Add("PRAGMA " + pragma.Key.Trim() + "=" + pragma.Value + ";");
                    }
                }
            }

            if (statements.Count == 0)
            {
                return;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Join(Environment.NewLine, statements.ToArray());
                command.ExecuteNonQuery();
            }
        }

        /// <summary>Returns a deep copy of these settings.</summary>
        public SQLiteConnectionSettings Clone()
        {
            return new SQLiteConnectionSettings
            {
                DatabasePath = DatabasePath,
                ConnectionString = ConnectionString,
                OpenMode = OpenMode,
                Password = Password,
                JournalMode = JournalMode,
                Synchronous = Synchronous,
                TempStore = TempStore,
                BusyTimeoutMilliseconds = BusyTimeoutMilliseconds,
                CommandTimeoutSeconds = CommandTimeoutSeconds,
                EnforceForeignKeys = EnforceForeignKeys,
                CacheSizeKilobytes = CacheSizeKilobytes,
                Pooling = Pooling,
                CreateDirectoryIfMissing = CreateDirectoryIfMissing,
                ExtraPragmas = ExtraPragmas == null ? null : new Dictionary<string, string>(ExtraPragmas),
                Lock = Lock == null ? new SQLiteLockOptions() : Lock.Clone(),
                Retry = Retry == null ? new SQLiteRetryOptions() : Retry.Clone()
            };
        }

        private static string TryGetDataSource(string connectionString)
        {
            try
            {
                return new SqliteConnectionStringBuilder(connectionString).DataSource;
            }
            catch
            {
                return null;
            }
        }
    }
}
