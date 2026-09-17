using System.Activities;
using System.ComponentModel;
using System.Data;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Query
{
    /// <summary>
    /// Runs a SELECT and returns the result as a DataTable. Reads never take the writer lock, so any
    /// number of robots can read the same database at the same time while another one writes.
    /// </summary>
    [Category("SQLite")]
    [DisplayName("SQLite Execute Query")]
    [Description("Runs a SELECT statement and returns a DataTable. Safe to run in parallel with other readers and with a writer.")]
#if NET6_0_OR_GREATER
    [System.Activities.ViewModels.ViewModelClass(typeof(Shaker.SQLLiteDB.Activities.Design.SQLiteExecuteQueryViewModel))]
#endif
    public class SQLiteExecuteQuery : SQLiteStatementActivity<DataTable>
    {
        /// <summary>The rows the query returned, as a DataTable.</summary>
        [Category("Output")]
        [DisplayName("Data table")]
        [Description("The rows the query returned, as a DataTable.")]
        public new OutArgument<System.Data.DataTable> Result
        {
            get { return base.Result; }
            set { base.Result = value; }
        }

        public SQLiteExecuteQuery()
        {
            DisplayName = "SQLite Execute Query";
        }

        [Category("Options")]
        [DisplayName("Column typing")]
        [Description("Auto derives each column type from the values returned, Declared trusts the provider, AllText returns every column as text.")]
        public SQLiteColumnTyping ColumnTyping { get; set; } = SQLiteColumnTyping.Auto;

        [Category("Options")]
        [DisplayName("Max rows")]
        [Description("Stop reading after this many rows. 0 means no limit.")]
        public InArgument<int> MaxRows { get; set; }

        [Category("Options")]
        [DisplayName("Result table name")]
        [Description("Name given to the resulting DataTable.")]
        public InArgument<string> ResultTableName { get; set; }

        [Category("Output")]
        [DisplayName("Row count")]
        [Description("Number of rows in the result.")]
        public OutArgument<int> RowCount { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var sql = Sql.Get(context);
            var parameters = GetValue(Parameters, context, null);
            var values = GetValue(ParameterValues, context, null);
            var maxRows = GetValue(MaxRows, context, 0);
            var tableName = GetValue(ResultTableName, context, null);
            var typing = ColumnTyping;

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireReadLock(cancellationToken))
                    {
                        return SQLiteCommandExecutor.ExecuteQuery(lease.Handle, sql, parameters, values, typing, maxRows, tableName, cancellationToken);
                    }
                },
                ApplyOutputs = (activityContext, table) => SetValue(RowCount, activityContext, table == null ? 0 : table.Rows.Count)
            };
        }
    }
}
