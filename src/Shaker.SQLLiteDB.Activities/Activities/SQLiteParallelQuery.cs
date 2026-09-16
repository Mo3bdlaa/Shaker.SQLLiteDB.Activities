using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Query
{
    /// <summary>
    /// Runs several SELECT statements at the same time, each on its own read only connection.
    /// In WAL mode readers never block each other or the writer, so this is the fast way to collect
    /// the data for a report.
    /// </summary>
    [Category("SQLite.Advanced")]
    [DisplayName("SQLite Parallel Query")]
    [Description("Runs several queries concurrently, each on its own read connection, and returns one DataTable per query.")]
    public class SQLiteParallelQuery : SQLiteActivityBase<Dictionary<string, DataTable>>
    {
        public SQLiteParallelQuery()
        {
            DisplayName = "SQLite Parallel Query";
        }

        [Category("Input")]
        [DisplayName("Queries")]
        [Description("The queries to run, as a Dictionary(Of String, String): the key names the result, the value is the SELECT statement.")]
        [RequiredArgument]
        public InArgument<IDictionary<string, string>> Queries { get; set; }

        [Category("Options")]
        [DisplayName("Max parallel readers")]
        [Description("How many queries run at the same time. Default 4.")]
        public InArgument<int> MaxDegreeOfParallelism { get; set; } = 4;

        [Category("Options")]
        [DisplayName("Open read only")]
        [Description("Open the extra connections read only, so they can never take the writer lock.")]
        public bool OpenReadOnly { get; set; } = true;

        [Category("Options")]
        [DisplayName("Column typing")]
        [Description("Auto derives each column type from the values returned, Declared trusts the provider, AllText returns every column as text.")]
        public SQLiteColumnTyping ColumnTyping { get; set; } = SQLiteColumnTyping.Auto;

        [Category("Output")]
        [DisplayName("Total rows")]
        [Description("Sum of the row counts of all results.")]
        public OutArgument<int> TotalRows { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var queries = Queries.Get(context);
            if (queries == null || queries.Count == 0)
            {
                throw new SQLiteActivityException("SQLite Parallel Query was given no queries to run.");
            }

            // Every reader needs its own connection, so the settings are taken either from the enclosing
            // scope or from this activity, and a fresh connection is opened per query.
            var handle = GetValue(Connection, context, null) ?? SQLiteExecutionProperties.FindConnection(context);
            var settings = handle != null ? handle.Settings.Clone() : BuildSettings(context);

            if (OpenReadOnly && !settings.IsInMemory)
            {
                settings.OpenMode = SQLiteOpenMode.ReadOnly;
                settings.JournalMode = SQLiteJournalMode.Unchanged;
            }

            var parallelism = Math.Max(1, GetValue(MaxDegreeOfParallelism, context, 4));
            var typing = ColumnTyping;
            var items = new List<KeyValuePair<string, string>>(queries);

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    var results = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);
                    var errors = new List<string>();
                    var gate = new SemaphoreSlim(parallelism, parallelism);
                    var tasks = new List<Task>(items.Count);

                    foreach (var item in items)
                    {
                        var name = item.Key;
                        var sql = item.Value;

                        tasks.Add(Task.Factory.StartNew(() =>
                        {
                            gate.Wait(cancellationToken);

                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                using (var reader = SQLiteConnectionHandle.Open(settings))
                                {
                                    var table = SQLiteCommandExecutor.ExecuteQuery(reader, sql, null, null, typing, 0, name, cancellationToken);

                                    lock (results)
                                    {
                                        results[name] = table;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                lock (errors)
                                {
                                    errors.Add(string.Format(CultureInfo.InvariantCulture, "Query '{0}' failed: {1}", name, ex.Message));
                                }
                            }
                            finally
                            {
                                gate.Release();
                            }
                        }, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
                    }

                    Task.WaitAll(tasks.ToArray(), cancellationToken);

                    if (errors.Count > 0)
                    {
                        throw new SQLiteActivityException(string.Join(Environment.NewLine, errors.ToArray()));
                    }

                    return results;
                },
                ApplyOutputs = (activityContext, results) =>
                {
                    var total = 0;
                    if (results != null)
                    {
                        foreach (var table in results.Values)
                        {
                            total += table.Rows.Count;
                        }
                    }

                    SetValue(TotalRows, activityContext, total);
                }
            };
        }
    }
}
