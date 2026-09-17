#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="Shaker.SQLLiteDB.Activities.Activities.Export.SQLiteImportCsv"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class SQLiteImportCsvViewModel : DesignPropertiesViewModel
    {
        public SQLiteImportCsvViewModel(IDesignServices services)
            : base(services)
        {
        }

        public DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle> Connection { get; set; } = new DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle>();

        public DesignInArgument<string> DatabasePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> FilePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> TableName { get; set; } = new DesignInArgument<string>();

        public DesignOutArgument<int> Result { get; set; } = new DesignOutArgument<int>();

        public DesignInArgument<string> ConnectionString { get; set; } = new DesignInArgument<string>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode> OpenMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode> JournalMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>();

        public DesignInArgument<int> BusyTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<string> Password { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> Delimiter { get; set; } = new DesignInArgument<string>();

        public DesignProperty<bool> HasHeaders { get; set; } = new DesignProperty<bool>();

        public DesignInArgument<string> Encoding { get; set; } = new DesignInArgument<string>();

        public DesignProperty<bool> DetectColumnTypes { get; set; } = new DesignProperty<bool>();

        public DesignProperty<bool> CreateTableIfNotExists { get; set; } = new DesignProperty<bool>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteConflictPolicy> ConflictPolicy { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteConflictPolicy>();

        public DesignInArgument<System.Collections.Generic.IEnumerable<string>> KeyColumns { get; set; } = new DesignInArgument<System.Collections.Generic.IEnumerable<string>>();

        public DesignInArgument<int> BatchSize { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> MaxRows { get; set; } = new DesignInArgument<int>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope> LockScope { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>();

        public DesignInArgument<string> LockFilePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<int> LockTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> TimeoutMS { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        public DesignOutArgument<System.Data.DataTable> ImportedData { get; set; } = new DesignOutArgument<System.Data.DataTable>();

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

            FilePath.DisplayName = "File path";
            FilePath.Tooltip = "The CSV file to read.";
            FilePath.Category = "Input";
            FilePath.IsPrincipal = true;
            FilePath.IsRequired = true;
            FilePath.OrderIndex = order++;
            FilePath.Widget = new DefaultWidget { Type = "Input" };

            TableName.DisplayName = "Table name";
            TableName.Tooltip = "Destination table.";
            TableName.Category = "Input";
            TableName.IsPrincipal = true;
            TableName.IsRequired = true;
            TableName.OrderIndex = order++;
            TableName.Widget = new DefaultWidget { Type = "Input" };

            Result.DisplayName = "Rows imported";
            Result.Tooltip = "How many rows the CSV added to the table.";
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

            Delimiter.DisplayName = "Delimiter";
            Delimiter.Tooltip = "Field separator. Default is a comma.";
            Delimiter.Category = "File format";
            Delimiter.IsVisible = false;
            Delimiter.OrderIndex = order++;
            Delimiter.Widget = new DefaultWidget { Type = "Input" };

            HasHeaders.DisplayName = "Has headers";
            HasHeaders.Tooltip = "The first line holds the column names.";
            HasHeaders.Category = "File format";
            HasHeaders.IsVisible = false;
            HasHeaders.OrderIndex = order++;
            HasHeaders.Widget = new DefaultWidget { Type = "Checkbox" };

            Encoding.DisplayName = "Encoding";
            Encoding.Tooltip = "Encoding name, for example utf-8 (default) or windows-1256.";
            Encoding.Category = "File format";
            Encoding.IsVisible = false;
            Encoding.OrderIndex = order++;
            Encoding.Widget = new DefaultWidget { Type = "Input" };

            DetectColumnTypes.DisplayName = "Detect column types";
            DetectColumnTypes.Tooltip = "Turn numbers, dates and booleans into typed columns instead of importing everything as text.";
            DetectColumnTypes.Category = "File format";
            DetectColumnTypes.IsVisible = false;
            DetectColumnTypes.OrderIndex = order++;
            DetectColumnTypes.Widget = new DefaultWidget { Type = "Checkbox" };

            CreateTableIfNotExists.DisplayName = "Create table if missing";
            CreateTableIfNotExists.Tooltip = "Create the destination table from the columns of the file when it does not exist yet.";
            CreateTableIfNotExists.Category = "Options";
            CreateTableIfNotExists.IsVisible = false;
            CreateTableIfNotExists.OrderIndex = order++;
            CreateTableIfNotExists.Widget = new DefaultWidget { Type = "Checkbox" };

            ConflictPolicy.DisplayName = "Conflict policy";
            ConflictPolicy.Tooltip = "How to react to constraint violations. Upsert needs 'Key columns'.";
            ConflictPolicy.Category = "Options";
            ConflictPolicy.IsVisible = false;
            ConflictPolicy.OrderIndex = order++;
            ConflictPolicy.Widget = new DefaultWidget { Type = "Dropdown" };
            ConflictPolicy.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteConflictPolicy>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            KeyColumns.DisplayName = "Key columns";
            KeyColumns.Tooltip = "Columns that identify an existing row for an UPSERT.";
            KeyColumns.Category = "Options";
            KeyColumns.IsVisible = false;
            KeyColumns.OrderIndex = order++;
            KeyColumns.Widget = new DefaultWidget { Type = "Input" };

            BatchSize.DisplayName = "Batch size";
            BatchSize.Tooltip = "Rows per transaction. Default 1000.";
            BatchSize.Category = "Options";
            BatchSize.IsVisible = false;
            BatchSize.OrderIndex = order++;
            BatchSize.Widget = new DefaultWidget { Type = "Input" };

            MaxRows.DisplayName = "Max rows";
            MaxRows.Tooltip = "Read at most this many rows from the file. 0 means all of them.";
            MaxRows.Category = "Options";
            MaxRows.IsVisible = false;
            MaxRows.OrderIndex = order++;
            MaxRows.Widget = new DefaultWidget { Type = "Input" };

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

            ImportedData.DisplayName = "Imported table";
            ImportedData.Tooltip = "The rows that were read from the file, handy for logging or checking.";
            ImportedData.Category = "Output";
            ImportedData.IsVisible = false;
            ImportedData.OrderIndex = order++;
            ImportedData.Widget = new DefaultWidget { Type = "Input" };

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
