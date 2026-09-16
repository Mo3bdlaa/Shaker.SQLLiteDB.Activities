using System.Activities;
using System.ComponentModel;
using System.IO;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Write
{
    /// <summary>
    /// Runs a script that contains several statements, taken from a string or from a .sql file.
    /// Useful for schema migrations and seed data.
    /// </summary>
    [Category("SQLite.Write")]
    [DisplayName("SQLite Execute Script")]
    [Description("Runs a multi statement SQL script from text or from a .sql file, optionally as one transaction.")]
    public class SQLiteExecuteScript : SQLiteActivityBase<int>
    {
        public SQLiteExecuteScript()
        {
            DisplayName = "SQLite Execute Script";
        }

        [Category("Input")]
        [DisplayName("Script")]
        [Description("The SQL script. Statements are separated by semicolons. Ignored when 'Script file path' is set.")]
        public InArgument<string> Script { get; set; }

        [Category("Input")]
        [DisplayName("Script file path")]
        [Description("Path of a .sql file to run. Wins over 'Script'.")]
        public InArgument<string> ScriptFilePath { get; set; }

        [Category("Options")]
        [DisplayName("Run in one transaction")]
        [Description("Wrap the whole script in one transaction, so a failure halfway leaves the database untouched.")]
        public bool UseTransaction { get; set; } = true;

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var script = GetValue(Script, context, null);
            var scriptFile = GetValue(ScriptFilePath, context, null);
            var useTransaction = UseTransaction;

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    var sql = script;

                    if (!string.IsNullOrWhiteSpace(scriptFile))
                    {
                        if (!File.Exists(scriptFile))
                        {
                            throw new SQLiteActivityException("The script file does not exist: " + scriptFile);
                        }

                        sql = File.ReadAllText(scriptFile);
                    }

                    if (string.IsNullOrWhiteSpace(sql))
                    {
                        throw new SQLiteActivityException("Neither 'Script' nor 'Script file path' contains anything to run.");
                    }

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

                        try
                        {
                            // A script is not safe to replay, so it runs without the automatic retry.
                            var affected = handle.Execute((connection, currentTransaction) =>
                            {
                                using (var command = SQLiteCommandExecutor.CreateCommand(connection, currentTransaction, sql, null, null, handle.Settings.CommandTimeoutSeconds))
                                {
                                    return command.ExecuteNonQuery();
                                }
                            }, cancellationToken, new SQLiteRetryOptions { MaxAttempts = 1 });

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
                }
            };
        }
    }
}
