using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Write
{
    /// <summary>
    /// Writes a whole DataTable into a table with one prepared statement and batched transactions.
    /// Supports insert, insert or ignore, replace and UPSERT (insert or update).
    /// </summary>
    [Category("SQLite.Write")]
    [DisplayName("SQLite Bulk Insert")]
    [Description("Writes a DataTable into a table in batches. Handles conflicts with ignore, replace or upsert, and can create the table from the DataTable.")]
    public class SQLiteBulkInsert : SQLiteActivityBase<int>
    {
        public SQLiteBulkInsert()
        {
            DisplayName = "SQLite Bulk Insert";
        }

        [Category("Input")]
        [DisplayName("Table name")]
        [Description("Destination table.")]
        [RequiredArgument]
        public InArgument<string> TableName { get; set; }

        [Category("Input")]
        [DisplayName("Data table")]
        [Description("Rows to write. Column names must match the destination columns, unless a column mapping is supplied.")]
        [RequiredArgument]
        public InArgument<DataTable> DataTable { get; set; }

        [Category("Options")]
        [DisplayName("Conflict policy")]
        [Description("Abort fails on a constraint violation, Ignore skips the row, Replace overwrites it, Upsert updates the existing row (needs 'Key columns').")]
        public SQLiteConflictPolicy ConflictPolicy { get; set; } = SQLiteConflictPolicy.Abort;

        [Category("Options")]
        [DisplayName("Key columns")]
        [Description("Columns that identify an existing row for an UPSERT. They must be covered by a primary key or a unique index.")]
        public InArgument<IEnumerable<string>> KeyColumns { get; set; }

        [Category("Options")]
        [DisplayName("Update columns")]
        [Description("Columns updated when an UPSERT hits an existing row. Empty updates every non key column.")]
        public InArgument<IEnumerable<string>> UpdateColumns { get; set; }

        [Category("Options")]
        [DisplayName("Batch size")]
        [Description("Rows per transaction. 0 writes everything in one transaction. Default 1000.")]
        public InArgument<int> BatchSize { get; set; } = 1000;

        [Category("Options")]
        [DisplayName("Column mapping")]
        [Description("Maps DataTable column names onto table column names, as a Dictionary(Of String, String).")]
        public InArgument<IDictionary<string, string>> ColumnMapping { get; set; }

        [Category("Options")]
        [DisplayName("Create table if missing")]
        [Description("Create the destination table from the shape of the DataTable when it does not exist yet.")]
        public bool CreateTableIfNotExists { get; set; }

        [Category("Options")]
        [DisplayName("Ignore extra columns")]
        [Description("Silently skip DataTable columns that the destination table does not have.")]
        public bool IgnoreExtraColumns { get; set; } = true;

        [Category("Output")]
        [DisplayName("Processed rows")]
        [Description("Rows that were sent to the database.")]
        public OutArgument<int> ProcessedRows { get; set; }

        [Category("Output")]
        [DisplayName("Skipped rows")]
        [Description("Rows the database did not write, for example because of INSERT OR IGNORE.")]
        public OutArgument<int> SkippedRows { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var tableName = TableName.Get(context);
            var data = DataTable.Get(context);

            var options = new SQLiteBulkOptions
            {
                ConflictPolicy = ConflictPolicy,
                KeyColumns = ToList(GetValue(KeyColumns, context, null)),
                UpdateColumns = ToList(GetValue(UpdateColumns, context, null)),
                BatchSize = GetValue(BatchSize, context, 1000),
                ColumnMapping = GetValue(ColumnMapping, context, null),
                CreateTableIfNotExists = CreateTableIfNotExists,
                IgnoreExtraColumns = IgnoreExtraColumns
            };

            var outcome = new SQLiteBulkResult[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireWriteLock(cancellationToken))
                    {
                        var result = SQLiteBulkWriter.Write(lease.Handle, tableName, data, options, cancellationToken);
                        outcome[0] = result;
                        return result.AffectedRows;
                    }
                },
                ApplyOutputs = (activityContext, affected) =>
                {
                    var result = outcome[0];
                    SetValue(ProcessedRows, activityContext, result == null ? 0 : result.ProcessedRows);
                    SetValue(SkippedRows, activityContext, result == null ? 0 : result.SkippedRows);
                }
            };
        }

        private static IList<string> ToList(IEnumerable<string> values)
        {
            if (values == null)
            {
                return null;
            }

            var list = new List<string>();
            foreach (var value in values)
            {
                list.Add(value);
            }

            return list;
        }
    }
}
