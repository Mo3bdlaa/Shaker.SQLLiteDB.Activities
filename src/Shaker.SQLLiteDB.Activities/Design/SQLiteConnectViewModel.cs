#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteConnect"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class SQLiteConnectViewModel : DesignPropertiesViewModel
    {
        public SQLiteConnectViewModel(IDesignServices services)
            : base(services)
        {
        }

        public DesignInArgument<string> DatabasePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> Password { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> LockFilePath { get; set; } = new DesignInArgument<string>();

        public DesignOutArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle> Connection { get; set; } = new DesignOutArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle>();

        public DesignInArgument<string> ConnectionString { get; set; } = new DesignInArgument<string>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode> OpenMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode> JournalMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>();

        public DesignProperty<bool> EnforceForeignKeys { get; set; } = new DesignProperty<bool>();

        public DesignInArgument<int> BusyTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope> LockScope { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>();

        public DesignInArgument<int> LockTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignOutArgument<string> SQLiteVersion { get; set; } = new DesignOutArgument<string>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var order = 1;

            DatabasePath.DisplayName = "Database path";
            DatabasePath.Tooltip = "Full path of the SQLite database file, for example C:\\Data\\orders.db. The file and its folder are created when they do not exist.";
            DatabasePath.Category = "Input";
            DatabasePath.IsPrincipal = true;
            DatabasePath.OrderIndex = order++;
            DatabasePath.Widget = new DefaultWidget { Type = "Input" };

            Password.DisplayName = "Password";
            Password.Tooltip = "Password of an encrypted database. Leave empty for a normal database.";
            Password.Category = "Input";
            Password.IsPrincipal = true;
            Password.OrderIndex = order++;
            Password.Widget = new DefaultWidget { Type = "Input" };

            LockFilePath.DisplayName = "Lock file path";
            LockFilePath.Tooltip = "Lock file that serializes writers. Empty uses '<database file>.writelock'.";
            LockFilePath.Category = "Locking";
            LockFilePath.IsPrincipal = true;
            LockFilePath.OrderIndex = order++;
            LockFilePath.Widget = new DefaultWidget { Type = "Input" };

            Connection.DisplayName = "Connection";
            Connection.Tooltip = "The open connection. Pass it to the other SQLite activities, and close it with SQLite Disconnect.";
            Connection.Category = "Output";
            Connection.IsPrincipal = true;
            Connection.OrderIndex = order++;
            Connection.Widget = new DefaultWidget { Type = "Input" };

            ConnectionString.DisplayName = "Connection string";
            ConnectionString.Tooltip = "Complete connection string, for full control. Wins over 'Database path'.";
            ConnectionString.Category = "Options";
            ConnectionString.IsVisible = false;
            ConnectionString.OrderIndex = order++;
            ConnectionString.Widget = new DefaultWidget { Type = "Input" };

            OpenMode.DisplayName = "Open mode";
            OpenMode.Tooltip = "ReadWriteCreate creates the database when missing, ReadOnly never takes the writer lock, Memory keeps everything in RAM.";
            OpenMode.Category = "Options";
            OpenMode.IsVisible = false;
            OpenMode.OrderIndex = order++;
            OpenMode.Widget = new DefaultWidget { Type = "Dropdown" };
            OpenMode.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            JournalMode.DisplayName = "Journal mode";
            JournalMode.Tooltip = "WAL lets many readers work while one writer is active. Recommended.";
            JournalMode.Category = "Options";
            JournalMode.IsVisible = false;
            JournalMode.OrderIndex = order++;
            JournalMode.Widget = new DefaultWidget { Type = "Dropdown" };
            JournalMode.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            EnforceForeignKeys.DisplayName = "Enforce foreign keys";
            EnforceForeignKeys.Tooltip = "Turn foreign key constraints on. SQLite has them off by default.";
            EnforceForeignKeys.Category = "Options";
            EnforceForeignKeys.IsVisible = false;
            EnforceForeignKeys.OrderIndex = order++;
            EnforceForeignKeys.Widget = new DefaultWidget { Type = "Checkbox" };

            BusyTimeoutMilliseconds.DisplayName = "Busy timeout (ms)";
            BusyTimeoutMilliseconds.Tooltip = "How long SQLite waits for a lock held by another connection before it reports 'database is locked'.";
            BusyTimeoutMilliseconds.Category = "Options";
            BusyTimeoutMilliseconds.IsVisible = false;
            BusyTimeoutMilliseconds.OrderIndex = order++;
            BusyTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            LockScope.DisplayName = "Lock scope";
            LockScope.Tooltip = "Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone.";
            LockScope.Category = "Locking";
            LockScope.IsVisible = false;
            LockScope.OrderIndex = order++;
            LockScope.Widget = new DefaultWidget { Type = "Dropdown" };
            LockScope.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            LockTimeoutMilliseconds.DisplayName = "Lock timeout (ms)";
            LockTimeoutMilliseconds.Tooltip = "How long a write activity waits for the writer lock before it fails.";
            LockTimeoutMilliseconds.Category = "Locking";
            LockTimeoutMilliseconds.IsVisible = false;
            LockTimeoutMilliseconds.OrderIndex = order++;
            LockTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            SQLiteVersion.DisplayName = "SQLite version";
            SQLiteVersion.Tooltip = "Version of the embedded SQLite engine, for example 3.45.1.";
            SQLiteVersion.Category = "Output";
            SQLiteVersion.IsVisible = false;
            SQLiteVersion.OrderIndex = order++;
            SQLiteVersion.Widget = new DefaultWidget { Type = "Input" };
        }
    }
}
#endif
