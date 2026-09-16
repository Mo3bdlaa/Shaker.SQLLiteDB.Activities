using System;
using System.Activities;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Maintenance
{
    /// <summary>
    /// Housekeeping for a SQLite database: VACUUM, ANALYZE, WAL checkpoint, integrity check and so on.
    /// Handy at the end of a long running process, or in a nightly job.
    /// </summary>
    [Category("SQLite.Maintenance")]
    [DisplayName("SQLite Maintenance")]
    [Description("Runs VACUUM, ANALYZE, PRAGMA optimize, a WAL checkpoint, an integrity check, a foreign key check or REINDEX.")]
#if NET6_0_OR_GREATER
    [System.Activities.ViewModels.ViewModelClass(typeof(Shaker.SQLLiteDB.Activities.Design.SQLiteMaintenanceViewModel))]
#endif
    public class SQLiteMaintenance : SQLiteActivityBase<string>
    {
        public SQLiteMaintenance()
        {
            DisplayName = "SQLite Maintenance";
        }

        [Category("Input")]
        [DisplayName("Operation")]
        [Description("Which maintenance command to run.")]
        public SQLiteMaintenanceOperation Operation { get; set; } = SQLiteMaintenanceOperation.Optimize;

        [Category("Output")]
        [DisplayName("Is healthy")]
        [Description("For the integrity and foreign key checks: true when the database reported no problem.")]
        public OutArgument<bool> IsHealthy { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var operation = Operation;
            var healthy = new bool[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireWriteLock(cancellationToken))
                    {
                        var handle = lease.Handle;

                        if (operation == SQLiteMaintenanceOperation.Vacuum && handle.CurrentTransaction != null && handle.CurrentTransaction.IsActive)
                        {
                            throw new SQLiteActivityException("VACUUM cannot run inside a transaction. Move the activity outside the SQLite Transaction Scope.");
                        }

                        var sql = BuildStatement(operation);

                        return handle.Execute((connection, transaction) =>
                        {
                            using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, sql, null, null, handle.Settings.CommandTimeoutSeconds))
                            using (var reader = command.ExecuteReader())
                            {
                                var output = new StringBuilder();

                                while (reader.Read())
                                {
                                    if (output.Length > 0)
                                    {
                                        output.AppendLine();
                                    }

                                    for (var i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (i > 0)
                                        {
                                            output.Append(" | ");
                                        }

                                        output.Append(reader.IsDBNull(i) ? string.Empty : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture));
                                    }
                                }

                                var text = output.ToString().Trim();

                                healthy[0] = operation == SQLiteMaintenanceOperation.IntegrityCheck
                                    ? string.Equals(text, "ok", StringComparison.OrdinalIgnoreCase)
                                    : operation != SQLiteMaintenanceOperation.ForeignKeyCheck || text.Length == 0;

                                if (text.Length == 0)
                                {
                                    text = operation + " completed.";
                                }

                                return text;
                            }
                        }, cancellationToken, new SQLiteRetryOptions { MaxAttempts = 1 });
                    }
                },
                ApplyOutputs = (activityContext, result) => SetValue(IsHealthy, activityContext, healthy[0])
            };
        }

        private static string BuildStatement(SQLiteMaintenanceOperation operation)
        {
            switch (operation)
            {
                case SQLiteMaintenanceOperation.Vacuum:
                    return "VACUUM;";
                case SQLiteMaintenanceOperation.Analyze:
                    return "ANALYZE;";
                case SQLiteMaintenanceOperation.Optimize:
                    return "PRAGMA optimize;";
                case SQLiteMaintenanceOperation.WalCheckpoint:
                    return "PRAGMA wal_checkpoint(TRUNCATE);";
                case SQLiteMaintenanceOperation.IntegrityCheck:
                    return "PRAGMA integrity_check;";
                case SQLiteMaintenanceOperation.ForeignKeyCheck:
                    return "PRAGMA foreign_key_check;";
                case SQLiteMaintenanceOperation.Reindex:
                    return "REINDEX;";
                default:
                    throw new SQLiteActivityException("Unsupported maintenance operation: " + operation);
            }
        }
    }
}
