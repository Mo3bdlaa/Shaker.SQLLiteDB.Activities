using System.Activities;
using System.ComponentModel;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Activities.Connection
{
    /// <summary>
    /// Opens a database and hands back the connection, the same way UiPath's Database Connect works.
    /// Pass the result to the other activities and close it with SQLite Disconnect when you are done.
    /// <para>
    /// No scope is involved: use this when you want the connection to live across a whole workflow, or
    /// to pass it between workflows. The SQLite Connect Scope remains available for people who prefer
    /// the connection to close itself.
    /// </para>
    /// </summary>
    [Category("SQLite.Connection")]
    [DisplayName("SQLite Connect")]
    [Description("Opens a SQLite database and returns the connection. No ODBC driver or SQLite installation is required.")]
#if NET6_0_OR_GREATER
    [System.Activities.ViewModels.ViewModelClass(typeof(Shaker.SQLLiteDB.Activities.Design.SQLiteConnectViewModel))]
#endif
    public class SQLiteConnect : CodeActivity
    {
        public SQLiteConnect()
        {
            DisplayName = "SQLite Connect";
        }

        #region Input

        /// <summary>Path of the database file.</summary>
        [Category("Input")]
        [DisplayName("Database path")]
        [Description("Full path of the SQLite database file, for example C:\\Data\\orders.db. The file and its folder are created when they do not exist.")]
        public InArgument<string> DatabasePath { get; set; }

        /// <summary>Password of an encrypted database.</summary>
        [Category("Input")]
        [DisplayName("Password")]
        [Description("Password of an encrypted database. Leave empty for a normal database.")]
        public InArgument<string> Password { get; set; }

        #endregion

        #region Options

        [Category("Options")]
        [DisplayName("Connection string")]
        [Description("Complete connection string, for full control. Wins over 'Database path'.")]
        public InArgument<string> ConnectionString { get; set; }

        [Category("Options")]
        [DisplayName("Open mode")]
        [Description("ReadWriteCreate creates the database when missing, ReadOnly never takes the writer lock, Memory keeps everything in RAM.")]
        public SQLiteOpenMode OpenMode { get; set; } = SQLiteOpenMode.ReadWriteCreate;

        [Category("Options")]
        [DisplayName("Journal mode")]
        [Description("WAL lets many readers work while one writer is active. Recommended.")]
        public SQLiteJournalMode JournalMode { get; set; } = SQLiteJournalMode.Wal;

        [Category("Options")]
        [DisplayName("Enforce foreign keys")]
        [Description("Turn foreign key constraints on. SQLite has them off by default.")]
        public bool EnforceForeignKeys { get; set; } = true;

        [Category("Options")]
        [DisplayName("Busy timeout (ms)")]
        [Description("How long SQLite waits for a lock held by another connection before it reports 'database is locked'.")]
        public InArgument<int> BusyTimeoutMilliseconds { get; set; } = 30000;

        #endregion

        #region Locking

        [Category("Locking")]
        [DisplayName("Lock scope")]
        [Description("Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone.")]
        public SQLiteLockScope LockScope { get; set; } = SQLiteLockScope.Machine;

        [Category("Locking")]
        [DisplayName("Lock file path")]
        [Description("Lock file that serializes writers. Empty uses '<database file>.writelock'.")]
        public InArgument<string> LockFilePath { get; set; }

        [Category("Locking")]
        [DisplayName("Lock timeout (ms)")]
        [Description("How long a write activity waits for the writer lock before it fails.")]
        public InArgument<int> LockTimeoutMilliseconds { get; set; } = 60000;

        #endregion

        #region Output

        /// <summary>The open connection. Hand it to the other activities, and to SQLite Disconnect.</summary>
        [Category("Output")]
        [DisplayName("Connection")]
        [Description("The open connection. Pass it to the other SQLite activities, and close it with SQLite Disconnect.")]
        public OutArgument<SQLiteConnectionHandle> Connection { get; set; }

        [Category("Output")]
        [DisplayName("SQLite version")]
        [Description("Version of the embedded SQLite engine, for example 3.45.1.")]
        public OutArgument<string> SQLiteVersion { get; set; }

        #endregion

        protected override void Execute(CodeActivityContext context)
        {
            var connectionString = ConnectionString == null ? null : ConnectionString.Get(context);
            var databasePath = DatabasePath == null ? null : DatabasePath.Get(context);

            if (string.IsNullOrWhiteSpace(connectionString) && string.IsNullOrWhiteSpace(databasePath))
            {
                throw new SQLiteActivityException("SQLite Connect needs either a 'Database path' or a 'Connection string'.");
            }

            var settings = new SQLiteConnectionSettings
            {
                DatabasePath = databasePath,
                ConnectionString = connectionString,
                OpenMode = OpenMode,
                JournalMode = JournalMode,
                EnforceForeignKeys = EnforceForeignKeys,
                Password = Password == null ? null : Password.Get(context),
                BusyTimeoutMilliseconds = BusyTimeoutMilliseconds == null ? 30000 : BusyTimeoutMilliseconds.Get(context),
                Lock = new SQLiteLockOptions
                {
                    Scope = LockScope,
                    LockFilePath = LockFilePath == null ? null : LockFilePath.Get(context),
                    AcquireTimeoutMilliseconds = LockTimeoutMilliseconds == null ? 60000 : LockTimeoutMilliseconds.Get(context)
                }
            };

            var handle = SQLiteConnectionHandle.Open(settings);

            if (Connection != null)
            {
                Connection.Set(context, handle);
            }

            if (SQLiteVersion != null)
            {
                SQLiteVersion.Set(context, SQLiteNative.EngineVersion);
            }
        }
    }
}
