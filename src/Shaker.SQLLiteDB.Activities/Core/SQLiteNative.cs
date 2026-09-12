using System;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>
    /// Bootstraps the embedded SQLite engine (SQLitePCLRaw / e_sqlite3) that ships inside this package.
    /// No ODBC driver, no System.Data.SQLite installation and no registry entry is needed on the robot.
    /// </summary>
    public static class SQLiteNative
    {
        private static int _initialized;
        private static string _version;

        /// <summary>
        /// Loads the bundled native SQLite library. Safe to call from several threads and as often as you like.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            {
                return;
            }

            try
            {
                SQLitePCL.Batteries_V2.Init();
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _initialized, 0);
                throw new SQLiteActivityException(
                    "The embedded SQLite engine (e_sqlite3) could not be loaded. Make sure the native files that ship " +
                    "with this package were copied next to the executing assembly (runtimes\\win-x64\\native\\e_sqlite3.dll " +
                    "or x86\\x64 sub folders for Windows - Legacy projects).", ex);
            }
        }

        /// <summary>Version string of the loaded SQLite engine, for example <c>3.45.1</c>.</summary>
        public static string EngineVersion
        {
            get
            {
                if (_version == null)
                {
                    EnsureInitialized();
                    using (var connection = new SqliteConnection("Data Source=:memory:"))
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "select sqlite_version();";
                            _version = Convert.ToString(command.ExecuteScalar());
                        }
                    }
                }

                return _version;
            }
        }

        /// <summary>
        /// Asks the engine to abort the statement that is currently running on <paramref name="connection"/>.
        /// Used to honour activity timeouts and workflow cancellation, because
        /// <see cref="SqliteCommand.Cancel"/> is a no-op in Microsoft.Data.Sqlite.
        /// </summary>
        internal static void Interrupt(SqliteConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            try
            {
                var handle = connection.Handle;
                if (handle != null)
                {
                    SQLitePCL.raw.sqlite3_interrupt(handle);
                }
            }
            catch
            {
                // Interrupting is best effort: never let it surface on top of the real error.
            }
        }
    }
}
