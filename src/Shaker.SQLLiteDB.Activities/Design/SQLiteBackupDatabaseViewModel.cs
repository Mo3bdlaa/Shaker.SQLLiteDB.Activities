#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteBackupDatabase"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class SQLiteBackupDatabaseViewModel : DesignPropertiesViewModel
    {
        public SQLiteBackupDatabaseViewModel(IDesignServices services)
            : base(services)
        {
        }

        public DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle> Connection { get; set; } = new DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle>();

        public DesignInArgument<string> DatabasePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> DestinationPath { get; set; } = new DesignInArgument<string>();

        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();

        public DesignInArgument<string> ConnectionString { get; set; } = new DesignInArgument<string>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode> OpenMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode> JournalMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>();

        public DesignInArgument<int> BusyTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<string> Password { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> BackupPassword { get; set; } = new DesignInArgument<string>();

        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope> LockScope { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>();

        public DesignInArgument<string> LockFilePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<int> LockTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> TimeoutMS { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        public DesignOutArgument<long> SizeInBytes { get; set; } = new DesignOutArgument<long>();

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

            DestinationPath.DisplayName = "Destination path";
            DestinationPath.Tooltip = "Where the copy is written. An existing file is replaced.";
            DestinationPath.Category = "Input";
            DestinationPath.IsPrincipal = true;
            DestinationPath.IsRequired = true;
            DestinationPath.OrderIndex = order++;
            DestinationPath.Widget = new DefaultWidget { Type = "Input" };

            Result.DisplayName = "Backup path";
            Result.Tooltip = "Full path of the copy that was written.";
            Result.Category = "Output";
            Result.IsPrincipal = true;
            Result.OrderIndex = order++;
            Result.Widget = new DefaultWidget { Type = "Input" };

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

            BackupPassword.DisplayName = "Backup password";
            BackupPassword.Tooltip = "Password of the copy. Empty keeps the password of the source, so an encrypted database is backed up encrypted.";
            BackupPassword.Category = "Options";
            BackupPassword.IsVisible = false;
            BackupPassword.OrderIndex = order++;
            BackupPassword.Widget = new DefaultWidget { Type = "Input" };

            Overwrite.DisplayName = "Overwrite";
            Overwrite.Tooltip = "Replace the destination file when it already exists.";
            Overwrite.Category = "Options";
            Overwrite.IsVisible = false;
            Overwrite.OrderIndex = order++;
            Overwrite.Widget = new DefaultWidget { Type = "Checkbox" };

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

            SizeInBytes.DisplayName = "Size (bytes)";
            SizeInBytes.Tooltip = "Size of the backup file that was written.";
            SizeInBytes.Category = "Output";
            SizeInBytes.IsVisible = false;
            SizeInBytes.OrderIndex = order++;
            SizeInBytes.Widget = new DefaultWidget { Type = "Input" };

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
