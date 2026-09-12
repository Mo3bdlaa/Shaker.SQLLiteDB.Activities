using System.Activities;
using System.ComponentModel;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>
    /// Runs an INSERT, UPDATE, DELETE or DDL statement and returns the number of affected rows.
    /// The writer lock is taken for the duration of the statement, unless an enclosing scope already holds it.
    /// </summary>
    [DisplayName("SQLite Execute Non Query")]
    [Description("Runs an INSERT, UPDATE, DELETE or DDL statement. Writers are serialized through the writer lock.")]
    public class SQLiteExecuteNonQuery : SQLiteStatementActivity<int>
    {
        public SQLiteExecuteNonQuery()
        {
            DisplayName = "SQLite Execute Non Query";
        }

        [Category("Output")]
        [DisplayName("Last insert row id")]
        [Description("ROWID of the row that was inserted last on this connection. 0 when the statement did not insert anything.")]
        public OutArgument<long> LastInsertRowId { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var sql = Sql.Get(context);
            var parameters = GetValue(Parameters, context, null);
            var values = GetValue(ParameterValues, context, null);
            var rowId = new long[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireWriteLock(cancellationToken))
                    {
                        return lease.Handle.Execute((connection, transaction) =>
                        {
                            int affected;

                            using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, sql, parameters, values, lease.Handle.Settings.CommandTimeoutSeconds))
                            {
                                affected = command.ExecuteNonQuery();
                            }

                            using (var rowIdCommand = connection.CreateCommand())
                            {
                                rowIdCommand.Transaction = transaction;
                                rowIdCommand.CommandText = "select last_insert_rowid();";
                                var result = rowIdCommand.ExecuteScalar();
                                rowId[0] = result == null || result == System.DBNull.Value
                                    ? 0
                                    : System.Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
                            }

                            return affected;
                        }, cancellationToken);
                    }
                },
                ApplyOutputs = (activityContext, affected) => SetValue(LastInsertRowId, activityContext, rowId[0])
            };
        }
    }
}
