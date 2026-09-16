#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="Shaker.SQLLiteDB.Activities.Activities.Export.SQLiteExportToJson"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class SQLiteExportToJsonViewModel : DesignPropertiesViewModel
    {
        public SQLiteExportToJsonViewModel(IDesignServices services)
            : base(services)
        {
        }

        public DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle> Connection { get; set; } = new DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle>();

        public DesignInArgument<string> DatabasePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> Sql { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> SourceTableName { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> FilePath { get; set; } = new DesignInArgument<string>();

        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();

        public DesignInArgument<System.Collections.Generic.IDictionary<string, object>> Parameters { get; set; } = new DesignInArgument<System.Collections.Generic.IDictionary<string, object>>();

        public DesignInArgument<System.Data.DataTable> DataTable { get; set; } = new DesignInArgument<System.Data.DataTable>();

        public DesignInArgument<string> ConnectionString { get; set; } = new DesignInArgument<string>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode> OpenMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteOpenMode>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode> JournalMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteJournalMode>();

        public DesignInArgument<int> BusyTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<string> Password { get; set; } = new DesignInArgument<string>();

        public DesignProperty<bool> Indented { get; set; } = new DesignProperty<bool>();

        public DesignInArgument<string> Encoding { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<string> DateTimeFormat { get; set; } = new DesignInArgument<string>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope> LockScope { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>();

        public DesignInArgument<string> LockFilePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<int> LockTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<int> TimeoutMS { get; set; } = new DesignInArgument<int>();

        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        public DesignOutArgument<int> RowsExported { get; set; } = new DesignOutArgument<int>();

        public DesignOutArgument<string> Json { get; set; } = new DesignOutArgument<string>();

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

            Sql.DisplayName = "SQL";
            Sql.Tooltip = "Query whose result is exported. Leave empty when 'Data table' is supplied.";
            Sql.Category = "Input";
            Sql.IsPrincipal = true;
            Sql.OrderIndex = order++;
            Sql.Widget = new DefaultWidget { Type = "TextComposer" };

            SourceTableName.DisplayName = "Table to export";
            SourceTableName.Tooltip = "Name of a table to export completely. A simpler alternative to writing a SELECT.";
            SourceTableName.Category = "Input";
            SourceTableName.IsPrincipal = true;
            SourceTableName.OrderIndex = order++;
            SourceTableName.Widget = new DefaultWidget { Type = "Input" };

            FilePath.DisplayName = "File path";
            FilePath.Tooltip = "Where the JSON file is written. Leave empty to get the JSON back as text in 'Result'.";
            FilePath.Category = "Output file";
            FilePath.IsPrincipal = true;
            FilePath.OrderIndex = order++;
            FilePath.Widget = new DefaultWidget { Type = "Input" };

            Result.DisplayName = "File written";
            Result.Tooltip = "Full path of the JSON file, empty when only the Json output was asked for.";
            Result.Category = "Output";
            Result.IsPrincipal = true;
            Result.OrderIndex = order++;
            Result.Widget = new DefaultWidget { Type = "Input" };

            Parameters.DisplayName = "Parameters";
            Parameters.Tooltip = "Named parameters of the query, as a Dictionary(Of String, Object).";
            Parameters.Category = "Input";
            Parameters.OrderIndex = order++;
            Parameters.Widget = new DefaultWidget { Type = "Input" };

            DataTable.DisplayName = "Data table";
            DataTable.Tooltip = "Rows to export. When this is supplied, no query is run and no connection is needed.";
            DataTable.Category = "Input";
            DataTable.OrderIndex = order++;
            DataTable.Widget = new DefaultWidget { Type = "Input" };

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

            Indented.DisplayName = "Indented";
            Indented.Tooltip = "Write the JSON with line breaks and indentation.";
            Indented.Category = "Output file";
            Indented.OrderIndex = order++;
            Indented.Widget = new DefaultWidget { Type = "Checkbox" };

            Encoding.DisplayName = "Encoding";
            Encoding.Tooltip = "Encoding name, for example utf-8 (default).";
            Encoding.Category = "Output file";
            Encoding.OrderIndex = order++;
            Encoding.Widget = new DefaultWidget { Type = "Input" };

            DateTimeFormat.DisplayName = "Date format";
            DateTimeFormat.Tooltip = "Format applied to date and time values. Default is the round trippable ISO 8601 format.";
            DateTimeFormat.Category = "Output file";
            DateTimeFormat.OrderIndex = order++;
            DateTimeFormat.Widget = new DefaultWidget { Type = "Input" };

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

            RowsExported.DisplayName = "Rows exported";
            RowsExported.Tooltip = "Number of rows written.";
            RowsExported.Category = "Output";
            RowsExported.OrderIndex = order++;
            RowsExported.Widget = new DefaultWidget { Type = "Input" };

            Json.DisplayName = "JSON";
            Json.Tooltip = "The JSON text. Also filled in when the result was written to a file, unless the result is very large.";
            Json.Category = "Output";
            Json.OrderIndex = order++;
            Json.Widget = new DefaultWidget { Type = "Input" };

            ErrorMessage.DisplayName = "Error message";
            ErrorMessage.Tooltip = "Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded.";
            ErrorMessage.Category = "Output";
            ErrorMessage.OrderIndex = order++;
            ErrorMessage.Widget = new DefaultWidget { Type = "Input" };
        }
    }
}
#endif
