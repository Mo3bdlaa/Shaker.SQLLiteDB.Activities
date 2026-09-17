using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Write
{
    /// <summary>
    /// Runs a list of write statements as one unit of work, holding the writer lock once for the whole
    /// batch. This is the activity to use when a workflow has to apply many different writes at once.
    /// </summary>
    [Category("SQLite.Advanced")]
    [DisplayName("SQLite Execute Statements")]
    [Description("Runs several write statements in one transaction while holding the writer lock once.")]
#if NET6_0_OR_GREATER
    [System.Activities.ViewModels.ViewModelClass(typeof(Shaker.SQLLiteDB.Activities.Design.SQLiteExecuteBatchViewModel))]
#endif
    public class SQLiteExecuteBatch : SQLiteActivityBase<int>
    {
        /// <summary>Total number of rows affected by the whole batch.</summary>
        [Category("Output")]
        [DisplayName("Affected rows")]
        [Description("Total number of rows affected by the whole batch.")]
        public new OutArgument<int> Result
        {
            get { return base.Result; }
            set { base.Result = value; }
        }

        public SQLiteExecuteBatch()
        {
            DisplayName = "SQLite Execute Statements";
        }

        [Category("Input")]
        [DisplayName("Statements")]
        [Description("Statements with their parameters, as a List(Of SQLiteStatement).")]
        public InArgument<IEnumerable<SQLiteStatement>> Statements { get; set; }

        [Category("Input")]
        [DisplayName("SQL statements")]
        [Description("Plain statements without parameters, as a List(Of String). Runs after 'Statements'.")]
        public InArgument<IEnumerable<string>> SqlStatements { get; set; }

        [Category("Options")]
        [DisplayName("Run in one transaction")]
        [Description("Commit the whole batch together. When one statement fails, nothing is written.")]
        public bool UseTransaction { get; set; } = true;

        [Category("Options")]
        [DisplayName("Stop on first error")]
        [Description("Stop at the first failing statement. When false, the batch carries on and reports the failures.")]
        public bool StopOnError { get; set; } = true;

        [Category("Output")]
        [DisplayName("Statements executed")]
        [Description("How many statements ran successfully.")]
        public OutArgument<int> StatementsExecuted { get; set; }

        [Category("Output")]
        [DisplayName("Failures")]
        [Description("Error message per failed statement, collected when 'Stop on first error' is off.")]
        public OutArgument<List<string>> Failures { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var statements = new List<SQLiteStatement>();

            var typed = GetValue(Statements, context, null);
            if (typed != null)
            {
                foreach (var statement in typed)
                {
                    if (statement != null && !string.IsNullOrWhiteSpace(statement.Sql))
                    {
                        statements.Add(statement);
                    }
                }
            }

            var plain = GetValue(SqlStatements, context, null);
            if (plain != null)
            {
                foreach (var sql in plain)
                {
                    if (!string.IsNullOrWhiteSpace(sql))
                    {
                        statements.Add(new SQLiteStatement(sql));
                    }
                }
            }

            if (statements.Count == 0)
            {
                throw new SQLiteActivityException("SQLite Execute Batch was given no statements to run.");
            }

            var useTransaction = UseTransaction;
            var stopOnError = StopOnError;
            var executed = new int[1];
            var failures = new List<string>();

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireWriteLock(cancellationToken))
                    {
                        var handle = lease.Handle;
                        var ownsTransaction = useTransaction && (handle.CurrentTransaction == null || !handle.CurrentTransaction.IsActive);
                        SQLiteTransactionHandle transaction = null;

                        if (ownsTransaction)
                        {
                            transaction = handle.BeginTransaction(SQLiteTransactionMode.Immediate);
                        }

                        var affected = 0;

                        try
                        {
                            for (var index = 0; index < statements.Count; index++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                var statement = statements[index];

                                try
                                {
                                    // A batch is not safe to replay, so the automatic retry is switched off here.
                                    affected += handle.Execute((connection, currentTransaction) =>
                                    {
                                        using (var command = SQLiteCommandExecutor.CreateCommand(connection, currentTransaction, statement.Sql, statement.Parameters, null, handle.Settings.CommandTimeoutSeconds))
                                        {
                                            return command.ExecuteNonQuery();
                                        }
                                    }, cancellationToken, new SQLiteRetryOptions { MaxAttempts = 1 });

                                    executed[0]++;
                                }
                                catch (Exception ex)
                                {
                                    var message = string.Format(CultureInfo.InvariantCulture,
                                        "Statement {0} of {1} failed: {2}", index + 1, statements.Count, ex.Message);

                                    if (stopOnError)
                                    {
                                        throw new SQLiteActivityException(message, ex);
                                    }

                                    failures.Add(message);
                                }
                            }

                            if (transaction != null)
                            {
                                transaction.Commit();
                            }

                            return affected;
                        }
                        catch
                        {
                            if (transaction != null)
                            {
                                transaction.Rollback();
                            }

                            throw;
                        }
                    }
                },
                ApplyOutputs = (activityContext, affected) =>
                {
                    SetValue(StatementsExecuted, activityContext, executed[0]);
                    SetValue(Failures, activityContext, failures);
                }
            };
        }
    }
}
