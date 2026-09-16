using System.Activities;
using System.ComponentModel;
using System.Data;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;
using Shaker.SQLLiteDB.Activities.IO;

namespace Shaker.SQLLiteDB.Activities.Activities.Export
{
    /// <summary>
    /// Exports a query result, a table or a DataTable to JSON, either into a file or straight into a
    /// String variable, ready to be posted to an API.
    /// </summary>
    [Category("SQLite.Advanced")]
    [DisplayName("SQLite Export To JSON")]
    [Description("Turns a query result into a JSON array of objects, written to a file or returned as text.")]
    public class SQLiteExportToJson : SQLiteExportBase<string>
    {
        public SQLiteExportToJson()
        {
            DisplayName = "SQLite Export To JSON";
        }

        [Category("Output file")]
        [DisplayName("File path")]
        [Description("Where the JSON file is written. Leave empty to get the JSON back as text in 'Result'.")]
        public InArgument<string> FilePath { get; set; }

        [Category("Output file")]
        [DisplayName("Indented")]
        [Description("Write the JSON with line breaks and indentation.")]
        public bool Indented { get; set; } = true;

        [Category("Output file")]
        [DisplayName("Encoding")]
        [Description("Encoding name, for example utf-8 (default).")]
        public InArgument<string> Encoding { get; set; }

        [Category("Output file")]
        [DisplayName("Date format")]
        [Description("Format applied to date and time values. Default is the round trippable ISO 8601 format.")]
        public InArgument<string> DateTimeFormat { get; set; }

        [Category("Output")]
        [DisplayName("Rows exported")]
        [Description("Number of rows written.")]
        public OutArgument<int> RowsExported { get; set; }

        [Category("Output")]
        [DisplayName("JSON")]
        [Description("The JSON text. Also filled in when the result was written to a file, unless the result is very large.")]
        public OutArgument<string> Json { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var path = GetValue(FilePath, context, null);
            var table = GetValue(DataTable, context, null);
            var sql = ResolveQuery(context);
            var parameters = GetValue(Parameters, context, null);
            var indented = Indented;
            var dateFormat = GetValue(DateTimeFormat, context, null);
            var encoding = ResolveEncoding(GetValue(Encoding, context, null));

            if (table == null && string.IsNullOrWhiteSpace(sql))
            {
                throw new SQLiteActivityException(
                    "SQLite Export To JSON needs something to export: set 'SQL', 'Table to export' or 'Data table'.");
            }

            var request = table == null ? ResolveConnection(context) : null;
            var rows = new int[1];
            var json = new string[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    var source = table;

                    if (source == null)
                    {
                        using (var lease = request.Acquire())
                        using (lease.Handle.AcquireReadLock(cancellationToken))
                        {
                            source = SQLiteCommandExecutor.ExecuteQuery(lease.Handle, sql, parameters, null, SQLiteColumnTyping.Auto, 0, "Result", cancellationToken);
                        }
                    }

                    rows[0] = source.Rows.Count;

                    if (string.IsNullOrWhiteSpace(path))
                    {
                        json[0] = JsonExporter.ToJson(source, indented, dateFormat);
                        return json[0];
                    }

                    JsonExporter.Write(source, path, indented, dateFormat, encoding, cancellationToken);

                    // Handing the text back as well is convenient, but not worth the memory for huge results.
                    if (source.Rows.Count <= 50000)
                    {
                        json[0] = JsonExporter.ToJson(source, indented, dateFormat);
                    }

                    return path;
                },
                ApplyOutputs = (activityContext, result) =>
                {
                    SetValue(RowsExported, activityContext, rows[0]);
                    SetValue(Json, activityContext, json[0]);
                }
            };
        }
    }
}
