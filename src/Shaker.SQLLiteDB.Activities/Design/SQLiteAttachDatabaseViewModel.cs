#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteAttachDatabase"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class SQLiteAttachDatabaseViewModel : DesignPropertiesViewModel
    {
        public SQLiteAttachDatabaseViewModel(IDesignServices services)
            : base(services)
        {
        }

        public DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle> Connection { get; set; } = new DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle>();

        public DesignInArgument<string> DatabasePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> AttachPath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> Alias { get; set; } = new DesignInArgument<string>();

        public DesignOutArgument<bool> Result { get; set; } = new DesignOutArgument<bool>();

        public DesignInArgument<string> ConnectionString { get; set; } = new DesignInArgument<string>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode> OpenMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode> JournalMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>();

        public DesignInArgument<int> BusyTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<string> Password { get; set; } = new DesignInArgument<string>();

        public DesignProperty<bool> Detach { get; set; } = new DesignProperty<bool>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope> LockScope { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>();

        public DesignInArgument<string> LockFilePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<int> LockTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> TimeoutMS { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        public DesignOutArgument<string> ErrorMessage { get; set; } = new DesignOutArgument<string>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var order = 1;

            Connection.DisplayName = "Connection";
            Connection.Tooltip = "Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead.";
            Connection.Category = "Connection";
            Connection.IsPrincipal = true;
            Connection.OrderIndex = order++;
            Connection.Widget = new DefaultWidget { Type = "Input" };

            DatabasePath.DisplayName = "Database path";
            DatabasePath.Tooltip = "Full path of the SQLite database file, for example C:\\Data\\orders.db. Ignored when a connection or a connection string is supplied.";
            DatabasePath.Category = "Connection";
            DatabasePath.IsPrincipal = true;
            DatabasePath.OrderIndex = order++;
            DatabasePath.Widget = new DefaultWidget { Type = "Input" };

            AttachPath.DisplayName = "Database to attach";
            AttachPath.Tooltip = "Path of the database file to attach.";
            AttachPath.Category = "Input";
            AttachPath.IsPrincipal = true;
            AttachPath.IsRequired = true;
            AttachPath.OrderIndex = order++;
            AttachPath.Widget = new DefaultWidget { Type = "Input" };

            Alias.DisplayName = "Alias";
            Alias.Tooltip = "Name the attached database gets in SQL, for example 'archive'.";
            Alias.Category = "Input";
            Alias.IsPrincipal = true;
            Alias.IsRequired = true;
            Alias.OrderIndex = order++;
            Alias.Widget = new DefaultWidget { Type = "Input" };

            Result.DisplayName = "Attached";
            Result.Tooltip = "True when the database was attached (or detached).";
            Result.Category = "Output";
            Result.IsPrincipal = true;
            Result.OrderIndex = order++;
            Result.Widget = new DefaultWidget { Type = "Checkbox" };

            ConnectionString.DisplayName = "Connection string";
            ConnectionString.Tooltip = "Complete connection string, for example \"Data Source=C:\\Data\\orders.db;Mode=ReadWriteCreate\". Wins over 'Database path'.";
            ConnectionString.Category = "Connection";
            ConnectionString.IsVisible = false;
            ConnectionString.OrderIndex = order++;
            ConnectionString.Widget = new DefaultWidget { Type = "Input" };

            OpenMode.DisplayName = "Open mode";
            OpenMode.Tooltip = "How the database file is opened when this activity opens its own connection.";
            OpenMode.Category = "Connection";
            OpenMode.IsVisible = false;
            OpenMode.OrderIndex = order++;
            OpenMode.Widget = new DefaultWidget { Type = "Dropdown" };
            OpenMode.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            JournalMode.DisplayName = "Journal mode";
            JournalMode.Tooltip = "WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting.";
            JournalMode.Category = "Connection";
            JournalMode.IsVisible = false;
            JournalMode.OrderIndex = order++;
            JournalMode.Widget = new DefaultWidget { Type = "Dropdown" };
            JournalMode.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            BusyTimeoutMilliseconds.DisplayName = "Busy timeout (ms)";
            BusyTimeoutMilliseconds.Tooltip = "How long SQLite waits for a lock held by another connection before it reports 'database is locked'.";
            BusyTimeoutMilliseconds.Category = "Connection";
            BusyTimeoutMilliseconds.IsVisible = false;
            BusyTimeoutMilliseconds.OrderIndex = order++;
            BusyTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            Password.DisplayName = "Password";
            Password.Tooltip = "Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password.";
            Password.Category = "Connection";
            Password.IsVisible = false;
            Password.OrderIndex = order++;
            Password.Widget = new DefaultWidget { Type = "Input" };

            Detach.DisplayName = "Detach instead";
            Detach.Tooltip = "Detach the alias again instead of attaching it.";
            Detach.Category = "Options";
            Detach.IsVisible = false;
            Detach.OrderIndex = order++;
            Detach.Widget = new DefaultWidget { Type = "Checkbox" };

            LockScope.DisplayName = "Lock scope";
            LockScope.Tooltip = "Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone.";
            LockScope.Category = "Locking";
            LockScope.IsVisible = false;
            LockScope.OrderIndex = order++;
            LockScope.Widget = new DefaultWidget { Type = "Dropdown" };
            LockScope.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            LockFilePath.DisplayName = "Lock file path";
            LockFilePath.Tooltip = "Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them.";
            LockFilePath.Category = "Locking";
            LockFilePath.IsVisible = false;
            LockFilePath.OrderIndex = order++;
            LockFilePath.Widget = new DefaultWidget { Type = "Input" };

            LockTimeoutMilliseconds.DisplayName = "Lock timeout (ms)";
            LockTimeoutMilliseconds.Tooltip = "How long this activity waits for the writer lock before it fails.";
            LockTimeoutMilliseconds.Category = "Locking";
            LockTimeoutMilliseconds.IsVisible = false;
            LockTimeoutMilliseconds.OrderIndex = order++;
            LockTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            TimeoutMS.DisplayName = "TimeoutMS";
            TimeoutMS.Tooltip = "Maximum run time of this activity in milliseconds. Use 0 for no limit.";
            TimeoutMS.Category = "Common";
            TimeoutMS.IsVisible = false;
            TimeoutMS.OrderIndex = order++;
            TimeoutMS.Widget = new DefaultWidget { Type = "Input" };

            ContinueOnError.DisplayName = "ContinueOnError";
            ContinueOnError.Tooltip = "When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'.";
            ContinueOnError.Category = "Common";
            ContinueOnError.IsVisible = false;
            ContinueOnError.OrderIndex = order++;
            ContinueOnError.Widget = new DefaultWidget { Type = "NullableBoolean" };

            ErrorMessage.DisplayName = "Error message";
            ErrorMessage.Tooltip = "Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded.";
            ErrorMessage.Category = "Output";
            ErrorMessage.IsVisible = false;
            ErrorMessage.OrderIndex = order++;
            ErrorMessage.Widget = new DefaultWidget { Type = "Input" };
        }
    }
}
#endif
