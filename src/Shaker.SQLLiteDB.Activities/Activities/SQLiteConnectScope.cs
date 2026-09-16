using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Connection
{
    /// <summary>
    /// Opens a SQLite database, runs the activities inside it and closes the connection afterwards,
    /// also when something fails. Every SQLite activity placed inside the scope picks the connection
    /// up automatically.
    /// </summary>
    [Category("SQLite.Connection")]
    [DisplayName("SQLite Connect Scope")]
    [Description("Opens a SQLite database and keeps the connection open for the activities inside. No ODBC driver or SQLite installation is required.")]
    public class SQLiteConnectScope : NativeActivity
    {
        private readonly Variable<SQLiteConnectionHandle> _handle = new Variable<SQLiteConnectionHandle>("ShakerSQLiteHandle");

        public SQLiteConnectScope()
        {
            DisplayName = "SQLite Connect Scope";
            Body = new ActivityAction<SQLiteConnectionHandle>
            {
                Argument = new DelegateInArgument<SQLiteConnectionHandle>("SQLiteConnection")
            };
        }

        /// <summary>Activities that run while the connection is open.</summary>
        [Browsable(false)]
        public ActivityAction<SQLiteConnectionHandle> Body { get; set; }

        #region Connection

        [Category("Connection")]
        [DisplayName("Database path")]
        [Description("Full path of the SQLite database file. The file, and its folder, are created when they do not exist yet.")]
        public InArgument<string> DatabasePath { get; set; }

        [Category("Connection")]
        [DisplayName("Connection string")]
        [Description("Complete connection string. Wins over 'Database path'.")]
        public InArgument<string> ConnectionString { get; set; }

        [Category("Connection")]
        [DisplayName("Open mode")]
        [Description("ReadWriteCreate creates the database when missing, ReadOnly never takes the writer lock, Memory keeps everything in RAM.")]
        public SQLiteOpenMode OpenMode { get; set; } = SQLiteOpenMode.ReadWriteCreate;

        [Category("Connection")]
        [DisplayName("Journal mode")]
        [Description("WAL lets many readers work while one writer is active. Recommended.")]
        public SQLiteJournalMode JournalMode { get; set; } = SQLiteJournalMode.Wal;

        [Category("Connection")]
        [DisplayName("Synchronous")]
        [Description("Durability level. Normal is safe and fast together with WAL.")]
        public SQLiteSynchronousMode Synchronous { get; set; } = SQLiteSynchronousMode.Normal;

        [Category("Connection")]
        [DisplayName("Enforce foreign keys")]
        [Description("Turn foreign key constraints on. SQLite has them off by default.")]
        public bool EnforceForeignKeys { get; set; } = true;

        [Category("Connection")]
        [DisplayName("Busy timeout (ms)")]
        [Description("How long SQLite waits for a lock held by another connection before it reports 'database is locked'.")]
        public InArgument<int> BusyTimeoutMilliseconds { get; set; } = 30000;

        [Category("Connection")]
        [DisplayName("Command timeout (s)")]
        [Description("Default command timeout in seconds for the activities inside the scope.")]
        public InArgument<int> CommandTimeoutSeconds { get; set; } = 60;

        [Category("Connection")]
        [DisplayName("Password")]
        [Description("Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password.")]
        public InArgument<string> Password { get; set; }

        [Category("Connection")]
        [DisplayName("Cache size (KB)")]
        [Description("Page cache size in kilobytes. 0 keeps the engine default.")]
        public InArgument<int> CacheSizeKilobytes { get; set; }

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

        [Category("Locking")]
        [DisplayName("Lock reads as well")]
        [Description("Also take the lock for read activities. Only needed for non-WAL databases on a file share.")]
        public bool LockReads { get; set; }

        #endregion

        #region Retry

        [Category("Retry")]
        [DisplayName("Retry attempts")]
        [Description("How often a statement is retried while SQLite reports 'database is locked'. 1 disables retrying.")]
        public InArgument<int> RetryAttempts { get; set; } = 5;

        [Category("Retry")]
        [DisplayName("Retry initial delay (ms)")]
        [Description("Delay before the first retry. It doubles after every failed attempt.")]
        public InArgument<int> RetryInitialDelayMilliseconds { get; set; } = 50;

        #endregion

        #region Output

        [Category("Output")]
        [DisplayName("Connection")]
        [Description("The connection that was opened, in case you want to hand it to activities outside the scope.")]
        public OutArgument<SQLiteConnectionHandle> Connection { get; set; }

        [Category("Output")]
        [DisplayName("SQLite version")]
        [Description("Version of the embedded SQLite engine, for example 3.45.1.")]
        public OutArgument<string> SQLiteVersion { get; set; }

        [Category("Output")]
        [DisplayName("Cipher version")]
        [Description("Version of the SQLCipher layer, for example '4.5.2 community'. Empty when the loaded engine cannot do encryption.")]
        public OutArgument<string> CipherVersion { get; set; }

        #endregion

        protected override bool CanInduceIdle
        {
            get { return true; }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            metadata.AddImplementationVariable(_handle);
        }

        protected override void Execute(NativeActivityContext context)
        {
            var settings = BuildSettings(context);
            var handle = SQLiteConnectionHandle.Open(settings);

            _handle.Set(context, handle);
            context.Properties.Add(SQLiteExecutionProperties.Connection, handle);

            if (Connection != null)
            {
                Connection.Set(context, handle);
            }

            if (SQLiteVersion != null)
            {
                SQLiteVersion.Set(context, SQLiteNative.EngineVersion);
            }

            if (CipherVersion != null)
            {
                CipherVersion.Set(context, SQLiteNative.CipherVersion ?? string.Empty);
            }

            if (Body == null)
            {
                Close(context);
                return;
            }

            context.ScheduleAction(Body, handle, OnBodyCompleted, OnBodyFaulted);
        }

        private SQLiteConnectionSettings BuildSettings(NativeActivityContext context)
        {
            var connectionString = ConnectionString == null ? null : ConnectionString.Get(context);
            var databasePath = DatabasePath == null ? null : DatabasePath.Get(context);

            if (string.IsNullOrWhiteSpace(connectionString) && string.IsNullOrWhiteSpace(databasePath))
            {
                throw new SQLiteActivityException(
                    "SQLite Connect Scope needs either a 'Database path' or a 'Connection string'.");
            }

            return new SQLiteConnectionSettings
            {
                DatabasePath = databasePath,
                ConnectionString = connectionString,
                OpenMode = OpenMode,
                JournalMode = JournalMode,
                Synchronous = Synchronous,
                EnforceForeignKeys = EnforceForeignKeys,
                Password = Password == null ? null : Password.Get(context),
                BusyTimeoutMilliseconds = BusyTimeoutMilliseconds == null ? 30000 : BusyTimeoutMilliseconds.Get(context),
                CommandTimeoutSeconds = CommandTimeoutSeconds == null ? 60 : CommandTimeoutSeconds.Get(context),
                CacheSizeKilobytes = CacheSizeKilobytes == null ? 0 : CacheSizeKilobytes.Get(context),
                Lock = new SQLiteLockOptions
                {
                    Scope = LockScope,
                    LockFilePath = LockFilePath == null ? null : LockFilePath.Get(context),
                    AcquireTimeoutMilliseconds = LockTimeoutMilliseconds == null ? 60000 : LockTimeoutMilliseconds.Get(context),
                    LockReads = LockReads
                },
                Retry = new SQLiteRetryOptions
                {
                    MaxAttempts = RetryAttempts == null ? 5 : RetryAttempts.Get(context),
                    InitialDelayMilliseconds = RetryInitialDelayMilliseconds == null ? 50 : RetryInitialDelayMilliseconds.Get(context)
                }
            };
        }

        private void OnBodyCompleted(NativeActivityContext context, ActivityInstance instance)
        {
            Close(context);
        }

        private void OnBodyFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            Close(faultContext);
        }

        private void Close(NativeActivityContext context)
        {
            var handle = _handle.Get(context);
            if (handle == null)
            {
                return;
            }

            _handle.Set(context, null);
            handle.Dispose();
        }

        protected override void Cancel(NativeActivityContext context)
        {
            base.Cancel(context);
            Close(context);
        }

        protected override void Abort(NativeActivityAbortContext context)
        {
            var handle = _handle.Get(context);
            if (handle != null)
            {
                handle.Dispose();
            }

            base.Abort(context);
        }
    }
}
