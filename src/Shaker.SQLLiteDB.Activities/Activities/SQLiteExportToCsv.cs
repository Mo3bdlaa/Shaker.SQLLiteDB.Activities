using System.Activities;
using System.ComponentModel;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;
using Shaker.SQLLiteDB.Activities.IO;

namespace Shaker.SQLLiteDB.Activities.Activities.Export
{
    /// <summary>
    /// Exports a query result, a table or a DataTable to a CSV file. The rows are streamed, so files of
    /// any size can be written without loading everything into memory.
    /// </summary>
    [Category("SQLite.Export")]
    [DisplayName("SQLite Export To CSV")]
    [Description("Writes a query result, a whole table or a DataTable to a CSV file, streaming row by row.")]
    public class SQLiteExportToCsv : SQLiteExportBase<string>
    {
        public SQLiteExportToCsv()
        {
            DisplayName = "SQLite Export To CSV";
        }

        [Category("Output file")]
        [DisplayName("File path")]
        [Description("Where the CSV file is written. Missing folders are created.")]
        [RequiredArgument]
        public InArgument<string> FilePath { get; set; }

        [Category("Output file")]
        [DisplayName("Delimiter")]
        [Description("Field separator. Default is a comma; use \";\" or vbTab for other conventions.")]
        public InArgument<string> Delimiter { get; set; } = ",";

        [Category("Output file")]
        [DisplayName("Include headers")]
        [Description("Write the column names as the first line.")]
        public bool IncludeHeaders { get; set; } = true;

        [Category("Output file")]
        [DisplayName("Quote all fields")]
        [Description("Wrap every field in quotes, not only the ones that need it.")]
        public bool QuoteAllFields { get; set; }

        [Category("Output file")]
        [DisplayName("Encoding")]
        [Description("Encoding name, for example utf-8 (default), utf-16 or windows-1256.")]
        public InArgument<string> Encoding { get; set; }

        [Category("Output file")]
        [DisplayName("Date format")]
        [Description("Format applied to date and time values. Default yyyy-MM-dd HH:mm:ss.")]
        public InArgument<string> DateTimeFormat { get; set; }

        [Category("Output file")]
        [DisplayName("Append")]
        [Description("Append to an existing file instead of replacing it. Headers are only written for a new file.")]
        public bool Append { get; set; }

        [Category("Output")]
        [DisplayName("Rows exported")]
        [Description("Number of data rows written to the file.")]
        public OutArgument<int> RowsExported { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var path = FilePath.Get(context);
            var table = GetValue(DataTable, context, null);
            var sql = ResolveQuery(context);
            var parameters = GetValue(Parameters, context, null);
            var append = Append;

            var format = new CsvFormat
            {
                Delimiter = GetValue(Delimiter, context, ","),
                IncludeHeaders = IncludeHeaders,
                QuoteAllFields = QuoteAllFields,
                Encoding = ResolveEncoding(GetValue(Encoding, context, null)),
                DateTimeFormat = GetValue(DateTimeFormat, context, null) ?? "yyyy-MM-dd HH:mm:ss"
            };

            if (table == null && string.IsNullOrWhiteSpace(sql))
            {
                throw new SQLiteActivityException(
                    "SQLite Export To CSV needs something to export: set 'SQL', 'Table to export' or 'Data table'.");
            }

            var request = table == null ? ResolveConnection(context) : null;
            var rows = new int[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    if (table != null)
                    {
                        rows[0] = CsvExporter.Write(table, path, format, append, cancellationToken);
                        return path;
                    }

                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireReadLock(cancellationToken))
                    {
                        rows[0] = lease.Handle.Execute((connection, transaction) =>
                        {
                            using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, sql, parameters, null, lease.Handle.Settings.CommandTimeoutSeconds))
                            using (var reader = command.ExecuteReader())
                            {
                                return CsvExporter.Write(reader, path, format, append, cancellationToken);
                            }
                        }, cancellationToken);
                    }

                    return path;
                },
                ApplyOutputs = (activityContext, result) => SetValue(RowsExported, activityContext, rows[0])
            };
        }
    }
}
