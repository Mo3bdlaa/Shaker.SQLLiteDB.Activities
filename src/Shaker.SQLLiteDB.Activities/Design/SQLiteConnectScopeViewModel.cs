#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteConnectScope"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class SQLiteConnectScopeViewModel : DesignPropertiesViewModel
    {
        public SQLiteConnectScopeViewModel(IDesignServices services)
            : base(services)
        {
        }

        public DesignInArgument<string> DatabasePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> Password { get; set; } = new DesignInArgument<string>();

        public DesignOutArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle> Connection { get; set; } = new DesignOutArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle>();

        public DesignInArgument<string> ConnectionString { get; set; } = new DesignInArgument<string>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode> OpenMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode> JournalMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteSynchronousMode> Synchronous { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteSynchronousMode>();

        public DesignProperty<bool> EnforceForeignKeys { get; set; } = new DesignProperty<bool>();

        public DesignInArgument<int> BusyTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> CommandTimeoutSeconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> CacheSizeKilobytes { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> RetryAttempts { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> RetryInitialDelayMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope> LockScope { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>();

        public DesignInArgument<string> LockFilePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<int> LockTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignProperty<bool> LockReads { get; set; } = new DesignProperty<bool>();

        public DesignOutArgument<string> SQLiteVersion { get; set; } = new DesignOutArgument<string>();

        public DesignOutArgument<string> CipherVersion { get; set; } = new DesignOutArgument<string>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var order = 1;

            DatabasePath.DisplayName = "Database path";
            DatabasePath.Tooltip = "Full path of the SQLite database file. The file, and its folder, are created when they do not exist yet.";
            DatabasePath.Category = "Connection";
            DatabasePath.IsPrincipal = true;
            DatabasePath.OrderIndex = order++;
            DatabasePath.Widget = new DefaultWidget { Type = "Input" };

            Password.DisplayName = "Password";
            Password.Tooltip = "Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password.";
            Password.Category = "Connection";
            Password.IsPrincipal = true;
            Password.OrderIndex = order++;
            Password.Widget = new DefaultWidget { Type = "Input" };

            Connection.DisplayName = "Connection";
            Connection.Tooltip = "The connection that was opened, in case you want to hand it to activities outside the scope.";
            Connection.Category = "Output";
            Connection.IsPrincipal = true;
            Connection.OrderIndex = order++;
            Connection.Widget = new DefaultWidget { Type = "Input" };

            ConnectionString.DisplayName = "Connection string";
            ConnectionString.Tooltip = "Complete connection string. Wins over 'Database path'.";
            ConnectionString.Category = "Connection";
            ConnectionString.OrderIndex = order++;
            ConnectionString.Widget = new DefaultWidget { Type = "Input" };

            OpenMode.DisplayName = "Open mode";
            OpenMode.Tooltip = "ReadWriteCreate creates the database when missing, ReadOnly never takes the writer lock, Memory keeps everything in RAM.";
            OpenMode.Category = "Connection";
            OpenMode.OrderIndex = order++;
            OpenMode.Widget = new DefaultWidget { Type = "Dropdown" };
            OpenMode.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            JournalMode.DisplayName = "Journal mode";
            JournalMode.Tooltip = "WAL lets many readers work while one writer is active. Recommended.";
            JournalMode.Category = "Connection";
            JournalMode.OrderIndex = order++;
            JournalMode.Widget = new DefaultWidget { Type = "Dropdown" };
            JournalMode.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            Synchronous.DisplayName = "Synchronous";
            Synchronous.Tooltip = "Durability level. Normal is safe and fast together with WAL.";
            Synchronous.Category = "Connection";
            Synchronous.OrderIndex = order++;
            Synchronous.Widget = new DefaultWidget { Type = "Dropdown" };
            Synchronous.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteSynchronousMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            EnforceForeignKeys.DisplayName = "Enforce foreign keys";
            EnforceForeignKeys.Tooltip = "Turn foreign key constraints on. SQLite has them off by default.";
            EnforceForeignKeys.Category = "Connection";
            EnforceForeignKeys.OrderIndex = order++;
            EnforceForeignKeys.Widget = new DefaultWidget { Type = "Checkbox" };

            BusyTimeoutMilliseconds.DisplayName = "Busy timeout (ms)";
            BusyTimeoutMilliseconds.Tooltip = "How long SQLite waits for a lock held by another connection before it reports 'database is locked'.";
            BusyTimeoutMilliseconds.Category = "Connection";
            BusyTimeoutMilliseconds.OrderIndex = order++;
            BusyTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            CommandTimeoutSeconds.DisplayName = "Command timeout (s)";
            CommandTimeoutSeconds.Tooltip = "Default command timeout in seconds for the activities inside the scope.";
            CommandTimeoutSeconds.Category = "Connection";
            CommandTimeoutSeconds.OrderIndex = order++;
            CommandTimeoutSeconds.Widget = new DefaultWidget { Type = "Input" };

            CacheSizeKilobytes.DisplayName = "Cache size (KB)";
            CacheSizeKilobytes.Tooltip = "Page cache size in kilobytes. 0 keeps the engine default.";
            CacheSizeKilobytes.Category = "Connection";
            CacheSizeKilobytes.OrderIndex = order++;
            CacheSizeKilobytes.Widget = new DefaultWidget { Type = "Input" };

            RetryAttempts.DisplayName = "Retry attempts";
            RetryAttempts.Tooltip = "How often a statement is retried while SQLite reports 'database is locked'. 1 disables retrying.";
            RetryAttempts.Category = "Retry";
            RetryAttempts.OrderIndex = order++;
            RetryAttempts.Widget = new DefaultWidget { Type = "Input" };

            RetryInitialDelayMilliseconds.DisplayName = "Retry initial delay (ms)";
            RetryInitialDelayMilliseconds.Tooltip = "Delay before the first retry. It doubles after every failed attempt.";
            RetryInitialDelayMilliseconds.Category = "Retry";
            RetryInitialDelayMilliseconds.OrderIndex = order++;
            RetryInitialDelayMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            LockScope.DisplayName = "Lock scope";
            LockScope.Tooltip = "Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone.";
            LockScope.Category = "Locking";
            LockScope.OrderIndex = order++;
            LockScope.Widget = new DefaultWidget { Type = "Dropdown" };
            LockScope.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            LockFilePath.DisplayName = "Lock file path";
            LockFilePath.Tooltip = "Lock file that serializes writers. Empty uses '<database file>.writelock'.";
            LockFilePath.Category = "Locking";
            LockFilePath.OrderIndex = order++;
            LockFilePath.Widget = new DefaultWidget { Type = "Input" };

            LockTimeoutMilliseconds.DisplayName = "Lock timeout (ms)";
            LockTimeoutMilliseconds.Tooltip = "How long a write activity waits for the writer lock before it fails.";
            LockTimeoutMilliseconds.Category = "Locking";
            LockTimeoutMilliseconds.OrderIndex = order++;
            LockTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            LockReads.DisplayName = "Lock reads as well";
            LockReads.Tooltip = "Also take the lock for read activities. Only needed for non-WAL databases on a file share.";
            LockReads.Category = "Locking";
            LockReads.OrderIndex = order++;
            LockReads.Widget = new DefaultWidget { Type = "Checkbox" };

            SQLiteVersion.DisplayName = "SQLite version";
            SQLiteVersion.Tooltip = "Version of the embedded SQLite engine, for example 3.45.1.";
            SQLiteVersion.Category = "Output";
            SQLiteVersion.OrderIndex = order++;
            SQLiteVersion.Widget = new DefaultWidget { Type = "Input" };

            CipherVersion.DisplayName = "Cipher version";
            CipherVersion.Tooltip = "Version of the SQLCipher layer, for example '4.5.2 community'. Empty when the loaded engine cannot do encryption.";
            CipherVersion.Category = "Output";
            CipherVersion.OrderIndex = order++;
            CipherVersion.Widget = new DefaultWidget { Type = "Input" };
        }
    }
}
#endif
