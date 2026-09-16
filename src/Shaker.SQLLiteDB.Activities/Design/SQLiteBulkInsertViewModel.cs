#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="Shaker.SQLLiteDB.Activities.Activities.Write.SQLiteBulkInsert"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class SQLiteBulkInsertViewModel : DesignPropertiesViewModel
    {
        public SQLiteBulkInsertViewModel(IDesignServices services)
            : base(services)
        {
        }

        public DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle> Connection { get; set; } = new DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle>();

        public DesignInArgument<string> DatabasePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> TableName { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<System.Data.DataTable> DataTable { get; set; } = new DesignInArgument<System.Data.DataTable>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteConflictPolicy> ConflictPolicy { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteConflictPolicy>();

        public DesignOutArgument<int> Result { get; set; } = new DesignOutArgument<int>();

        public DesignInArgument<string> ConnectionString { get; set; } = new DesignInArgument<string>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode> OpenMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode> JournalMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>();

        public DesignInArgument<int> BusyTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<string> Password { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<System.Collections.Generic.IEnumerable<string>> KeyColumns { get; set; } = new DesignInArgument<System.Collections.Generic.IEnumerable<string>>();

        public DesignInArgument<System.Collections.Generic.IEnumerable<string>> UpdateColumns { get; set; } = new DesignInArgument<System.Collections.Generic.IEnumerable<string>>();

        public DesignInArgument<int> BatchSize { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<System.Collections.Generic.IDictionary<string, string>> ColumnMapping { get; set; } = new DesignInArgument<System.Collections.Generic.IDictionary<string, string>>();

        public DesignProperty<bool> CreateTableIfNotExists { get; set; } = new DesignProperty<bool>();

        public DesignProperty<bool> IgnoreExtraColumns { get; set; } = new DesignProperty<bool>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope> LockScope { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>();

        public DesignInArgument<string> LockFilePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<int> LockTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> TimeoutMS { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        public DesignOutArgument<int> ProcessedRows { get; set; } = new DesignOutArgument<int>();

        public DesignOutArgument<int> SkippedRows { get; set; } = new DesignOutArgument<int>();

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

            TableName.DisplayName = "Table name";
            TableName.Tooltip = "Destination table.";
            TableName.Category = "Input";
            TableName.IsPrincipal = true;
            TableName.IsRequired = true;
            TableName.OrderIndex = order++;
            TableName.Widget = new DefaultWidget { Type = "Input" };

            DataTable.DisplayName = "Data table";
            DataTable.Tooltip = "Rows to write. Column names must match the destination columns, unless a column mapping is supplied.";
            DataTable.Category = "Input";
            DataTable.IsPrincipal = true;
            DataTable.IsRequired = true;
            DataTable.OrderIndex = order++;
            DataTable.Widget = new DefaultWidget { Type = "Input" };

            ConflictPolicy.DisplayName = "Conflict policy";
            ConflictPolicy.Tooltip = "Abort fails on a constraint violation, Ignore skips the row, Replace overwrites it, Upsert updates the existing row (needs 'Key columns').";
            ConflictPolicy.Category = "Options";
            ConflictPolicy.IsPrincipal = true;
            ConflictPolicy.OrderIndex = order++;
            ConflictPolicy.Widget = new DefaultWidget { Type = "Dropdown" };
            ConflictPolicy.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteConflictPolicy>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            Result.DisplayName = "Rows written";
            Result.Tooltip = "How many rows reached the table.";
            Result.Category = "Output";
            Result.IsPrincipal = true;
            Result.OrderIndex = order++;
            Result.Widget = new DefaultWidget { Type = "Input" };

            ConnectionString.DisplayName = "Connection string";
            ConnectionString.Tooltip = "Complete connection string, for example \"Data Source=C:\\Data\\orders.db;Mode=ReadWriteCreate\". Wins over 'Database path'.";
            ConnectionString.Category = "Connection";
            ConnectionString.OrderIndex = order++;
            ConnectionString.Widget = new DefaultWidget { Type = "Input" };

            OpenMode.DisplayName = "Open mode";
            OpenMode.Tooltip = "How the database file is opened when this activity opens its own connection.";
            OpenMode.Category = "Connection";
            OpenMode.OrderIndex = order++;
            OpenMode.Widget = new DefaultWidget { Type = "Dropdown" };
            OpenMode.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            JournalMode.DisplayName = "Journal mode";
            JournalMode.Tooltip = "WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting.";
            JournalMode.Category = "Connection";
            JournalMode.OrderIndex = order++;
            JournalMode.Widget = new DefaultWidget { Type = "Dropdown" };
            JournalMode.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            BusyTimeoutMilliseconds.DisplayName = "Busy timeout (ms)";
            BusyTimeoutMilliseconds.Tooltip = "How long SQLite waits for a lock held by another connection before it reports 'database is locked'.";
            BusyTimeoutMilliseconds.Category = "Connection";
            BusyTimeoutMilliseconds.OrderIndex = order++;
            BusyTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            Password.DisplayName = "Password";
            Password.Tooltip = "Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password.";
            Password.Category = "Connection";
            Password.OrderIndex = order++;
            Password.Widget = new DefaultWidget { Type = "Input" };

            KeyColumns.DisplayName = "Key columns";
            KeyColumns.Tooltip = "Columns that identify an existing row for an UPSERT. They must be covered by a primary key or a unique index.";
            KeyColumns.Category = "Options";
            KeyColumns.OrderIndex = order++;
            KeyColumns.Widget = new DefaultWidget { Type = "Input" };

            UpdateColumns.DisplayName = "Update columns";
            UpdateColumns.Tooltip = "Columns updated when an UPSERT hits an existing row. Empty updates every non key column.";
            UpdateColumns.Category = "Options";
            UpdateColumns.OrderIndex = order++;
            UpdateColumns.Widget = new DefaultWidget { Type = "Input" };

            BatchSize.DisplayName = "Batch size";
            BatchSize.Tooltip = "Rows per transaction. 0 writes everything in one transaction. Default 1000.";
            BatchSize.Category = "Options";
            BatchSize.OrderIndex = order++;
            BatchSize.Widget = new DefaultWidget { Type = "Input" };

            ColumnMapping.DisplayName = "Column mapping";
            ColumnMapping.Tooltip = "Maps DataTable column names onto table column names, as a Dictionary(Of String, String).";
            ColumnMapping.Category = "Options";
            ColumnMapping.OrderIndex = order++;
            ColumnMapping.Widget = new DefaultWidget { Type = "Input" };

            CreateTableIfNotExists.DisplayName = "Create table if missing";
            CreateTableIfNotExists.Tooltip = "Create the destination table from the shape of the DataTable when it does not exist yet.";
            CreateTableIfNotExists.Category = "Options";
            CreateTableIfNotExists.OrderIndex = order++;
            CreateTableIfNotExists.Widget = new DefaultWidget { Type = "Checkbox" };

            IgnoreExtraColumns.DisplayName = "Ignore extra columns";
            IgnoreExtraColumns.Tooltip = "Silently skip DataTable columns that the destination table does not have.";
            IgnoreExtraColumns.Category = "Options";
            IgnoreExtraColumns.OrderIndex = order++;
            IgnoreExtraColumns.Widget = new DefaultWidget { Type = "Checkbox" };

            LockScope.DisplayName = "Lock scope";
            LockScope.Tooltip = "Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone.";
            LockScope.Category = "Locking";
            LockScope.OrderIndex = order++;
            LockScope.Widget = new DefaultWidget { Type = "Dropdown" };
            LockScope.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            LockFilePath.DisplayName = "Lock file path";
            LockFilePath.Tooltip = "Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them.";
            LockFilePath.Category = "Locking";
            LockFilePath.OrderIndex = order++;
            LockFilePath.Widget = new DefaultWidget { Type = "Input" };

            LockTimeoutMilliseconds.DisplayName = "Lock timeout (ms)";
            LockTimeoutMilliseconds.Tooltip = "How long this activity waits for the writer lock before it fails.";
            LockTimeoutMilliseconds.Category = "Locking";
            LockTimeoutMilliseconds.OrderIndex = order++;
            LockTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            TimeoutMS.DisplayName = "TimeoutMS";
            TimeoutMS.Tooltip = "Maximum run time of this activity in milliseconds. Use 0 for no limit.";
            TimeoutMS.Category = "Common";
            TimeoutMS.OrderIndex = order++;
            TimeoutMS.Widget = new DefaultWidget { Type = "Input" };

            ContinueOnError.DisplayName = "ContinueOnError";
            ContinueOnError.Tooltip = "When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'.";
            ContinueOnError.Category = "Common";
            ContinueOnError.OrderIndex = order++;
            ContinueOnError.Widget = new DefaultWidget { Type = "NullableBoolean" };

            ProcessedRows.DisplayName = "Processed rows";
            ProcessedRows.Tooltip = "Rows that were sent to the database.";
            ProcessedRows.Category = "Output";
            ProcessedRows.OrderIndex = order++;
            ProcessedRows.Widget = new DefaultWidget { Type = "Input" };

            SkippedRows.DisplayName = "Skipped rows";
            SkippedRows.Tooltip = "Rows the database did not write, for example because of INSERT OR IGNORE.";
            SkippedRows.Category = "Output";
            SkippedRows.OrderIndex = order++;
            SkippedRows.Widget = new DefaultWidget { Type = "Input" };

            ErrorMessage.DisplayName = "Error message";
            ErrorMessage.Tooltip = "Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded.";
            ErrorMessage.Category = "Output";
            ErrorMessage.OrderIndex = order++;
            ErrorMessage.Widget = new DefaultWidget { Type = "Input" };
        }
    }
}
#endif
