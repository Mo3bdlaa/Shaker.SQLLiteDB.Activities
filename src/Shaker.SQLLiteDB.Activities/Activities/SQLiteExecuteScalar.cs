using System;
using System.Activities;
using System.ComponentModel;
using System.Globalization;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>Runs a statement and returns the first column of the first row, for example a COUNT(*).</summary>
    [DisplayName("SQLite Execute Scalar")]
    [Description("Runs a statement and returns a single value, for example the result of a COUNT or a MAX.")]
    public class SQLiteExecuteScalar : SQLiteStatementActivity<object>
    {
        public SQLiteExecuteScalar()
        {
            DisplayName = "SQLite Execute Scalar";
        }

        [Category("Output")]
        [DisplayName("Text result")]
        [Description("The value as text, which saves a conversion in the workflow.")]
        public OutArgument<string> TextResult { get; set; }

        [Category("Output")]
        [DisplayName("Number result")]
        [Description("The value as a number. 0 when the value is NULL or not numeric.")]
        public OutArgument<double> NumberResult { get; set; }

        [Category("Output")]
        [DisplayName("Is null")]
        [Description("True when the query returned NULL or no row at all.")]
        public OutArgument<bool> IsNull { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var sql = Sql.Get(context);
            var parameters = GetValue(Parameters, context, null);
            var values = GetValue(ParameterValues, context, null);

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireReadLock(cancellationToken))
                    {
                        return lease.Handle.Execute((connection, transaction) =>
                        {
                            using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, sql, parameters, values, lease.Handle.Settings.CommandTimeoutSeconds))
                            {
                                var result = command.ExecuteScalar();
                                return result == DBNull.Value ? null : result;
                            }
                        }, cancellationToken);
                    }
                },
                ApplyOutputs = (activityContext, result) =>
                {
                    SetValue(IsNull, activityContext, result == null);
                    SetValue(TextResult, activityContext, result == null ? null : Convert.ToString(result, CultureInfo.InvariantCulture));

                    double number = 0;
                    if (result != null)
                    {
                        try
                        {
                            number = Convert.ToDouble(result, CultureInfo.InvariantCulture);
                        }
                        catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                        {
                            number = 0;
                        }
                    }

                    SetValue(NumberResult, activityContext, number);
                }
            };
        }
    }
}
