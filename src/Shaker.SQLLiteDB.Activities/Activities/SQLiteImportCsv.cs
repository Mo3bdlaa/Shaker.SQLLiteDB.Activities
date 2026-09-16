using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;
using Shaker.SQLLiteDB.Activities.IO;

namespace Shaker.SQLLiteDB.Activities.Activities.Export
{
    /// <summary>Reads a CSV file and writes it into a table, creating the table when asked to.</summary>
    [Category("SQLite.Export")]
    [DisplayName("SQLite Import CSV")]
    [Description("Loads a CSV file into a table, in batches, with the same conflict handling as the bulk insert.")]
    public class SQLiteImportCsv : SQLiteActivityBase<int>
    {
        public SQLiteImportCsv()
        {
            DisplayName = "SQLite Import CSV";
        }

        [Category("Input")]
        [DisplayName("File path")]
        [Description("The CSV file to read.")]
        [RequiredArgument]
        public InArgument<string> FilePath { get; set; }

        [Category("Input")]
        [DisplayName("Table name")]
        [Description("Destination table.")]
        [RequiredArgument]
        public InArgument<string> TableName { get; set; }

        [Category("File format")]
        [DisplayName("Delimiter")]
        [Description("Field separator. Default is a comma.")]
        public InArgument<string> Delimiter { get; set; } = ",";

        [Category("File format")]
        [DisplayName("Has headers")]
        [Description("The first line holds the column names.")]
        public bool HasHeaders { get; set; } = true;

        [Category("File format")]
        [DisplayName("Encoding")]
        [Description("Encoding name, for example utf-8 (default) or windows-1256.")]
        public InArgument<string> Encoding { get; set; }

        [Category("File format")]
        [DisplayName("Detect column types")]
        [Description("Turn numbers, dates and booleans into typed columns instead of importing everything as text.")]
        public bool DetectColumnTypes { get; set; } = true;

        [Category("Options")]
        [DisplayName("Create table if missing")]
        [Description("Create the destination table from the columns of the file when it does not exist yet.")]
        public bool CreateTableIfNotExists { get; set; } = true;

        [Category("Options")]
        [DisplayName("Conflict policy")]
        [Description("How to react to constraint violations. Upsert needs 'Key columns'.")]
        public SQLiteConflictPolicy ConflictPolicy { get; set; } = SQLiteConflictPolicy.Abort;

        [Category("Options")]
        [DisplayName("Key columns")]
        [Description("Columns that identify an existing row for an UPSERT.")]
        public InArgument<IEnumerable<string>> KeyColumns { get; set; }

        [Category("Options")]
        [DisplayName("Batch size")]
        [Description("Rows per transaction. Default 1000.")]
        public InArgument<int> BatchSize { get; set; } = 1000;

        [Category("Options")]
        [DisplayName("Max rows")]
        [Description("Read at most this many rows from the file. 0 means all of them.")]
        public InArgument<int> MaxRows { get; set; }

        [Category("Output")]
        [DisplayName("Imported table")]
        [Description("The rows that were read from the file, handy for logging or checking.")]
        public OutArgument<DataTable> ImportedData { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var path = FilePath.Get(context);
            var tableName = TableName.Get(context);
            var maxRows = GetValue(MaxRows, context, 0);
            var detectTypes = DetectColumnTypes;

            var format = new CsvFormat
            {
                Delimiter = GetValue(Delimiter, context, ","),
                IncludeHeaders = HasHeaders,
                Encoding = ResolveEncoding(GetValue(Encoding, context, null))
            };

            var keyColumns = new List<string>();
            var keys = GetValue(KeyColumns, context, null);
            if (keys != null)
            {
                keyColumns.AddRange(keys);
            }

            var options = new SQLiteBulkOptions
            {
                ConflictPolicy = ConflictPolicy,
                KeyColumns = keyColumns.Count == 0 ? null : keyColumns,
                BatchSize = GetValue(BatchSize, context, 1000),
                CreateTableIfNotExists = CreateTableIfNotExists
            };

            var imported = new DataTable[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    var data = CsvImporter.Read(path, format, detectTypes, maxRows, cancellationToken);
                    imported[0] = data;

                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireWriteLock(cancellationToken))
                    {
                        var result = SQLiteBulkWriter.Write(lease.Handle, tableName, data, options, cancellationToken);
                        return result.AffectedRows;
                    }
                },
                ApplyOutputs = (activityContext, affected) => SetValue(ImportedData, activityContext, imported[0])
            };
        }

        private static System.Text.Encoding ResolveEncoding(string encodingName)
        {
            if (string.IsNullOrWhiteSpace(encodingName))
            {
                return new System.Text.UTF8Encoding(true);
            }

            try
            {
                return System.Text.Encoding.GetEncoding(encodingName.Trim());
            }
            catch (System.ArgumentException)
            {
                throw new SQLiteActivityException(
                    "Unknown encoding '" + encodingName + "'. Use for example utf-8, utf-16, windows-1256 or iso-8859-1.");
            }
        }
    }
}
