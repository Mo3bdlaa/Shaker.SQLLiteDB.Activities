using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;
using Shaker.SQLLiteDB.Activities.IO;

namespace Shaker.SQLLiteDB.Activities.Activities.Export
{
    /// <summary>
    /// Writes query results to a real .xlsx workbook. The file is produced by this package itself, so
    /// Excel does not have to be installed and no Open XML library is pulled into the project.
    /// Several queries can be written to one workbook, one worksheet each.
    /// </summary>
    [Category("SQLite.Export")]
    [DisplayName("SQLite Export To Excel")]
    [Description("Writes a query result, a table or a DataTable to an .xlsx workbook. No Excel installation and no extra library needed.")]
    public class SQLiteExportToExcel : SQLiteExportBase<string>
    {
        public SQLiteExportToExcel()
        {
            DisplayName = "SQLite Export To Excel";
        }

        [Category("Output file")]
        [DisplayName("File path")]
        [Description("Where the .xlsx file is written. Missing folders are created.")]
        [RequiredArgument]
        public InArgument<string> FilePath { get; set; }

        [Category("Output file")]
        [DisplayName("Sheet name")]
        [Description("Name of the worksheet. Default is Sheet1.")]
        public InArgument<string> SheetName { get; set; }

        [Category("Input")]
        [DisplayName("Sheets")]
        [Description("Several worksheets at once, as a Dictionary(Of String, String): the key is the sheet name, the value is the query.")]
        public InArgument<IDictionary<string, string>> Sheets { get; set; }

        [Category("Layout")]
        [DisplayName("Include headers")]
        [Description("Write the column names in the first row, in bold.")]
        public bool IncludeHeaders { get; set; } = true;

        [Category("Layout")]
        [DisplayName("Freeze header row")]
        [Description("Keep the header row visible while scrolling.")]
        public bool FreezeHeaderRow { get; set; } = true;

        [Category("Layout")]
        [DisplayName("Auto filter")]
        [Description("Add the filter dropdowns to the header row.")]
        public bool AutoFilter { get; set; } = true;

        [Category("Layout")]
        [DisplayName("Auto size columns")]
        [Description("Give the columns a width that fits the header and the first rows.")]
        public bool AutoSizeColumns { get; set; } = true;

        [Category("Layout")]
        [DisplayName("Date format")]
        [Description("Excel number format for date and time values. Default yyyy-mm-dd hh:mm:ss.")]
        public InArgument<string> DateTimeFormat { get; set; }

        [Category("Layout")]
        [DisplayName("Split large results")]
        [Description("Spread a result larger than 1,048,576 rows over several worksheets instead of failing.")]
        public bool SplitLargeTables { get; set; } = true;

        [Category("Output")]
        [DisplayName("Rows exported")]
        [Description("Total number of data rows written to the workbook.")]
        public OutArgument<int> RowsExported { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var path = FilePath.Get(context);
            var table = GetValue(DataTable, context, null);
            var sql = ResolveQuery(context);
            var parameters = GetValue(Parameters, context, null);
            var sheetName = GetValue(SheetName, context, null);
            var sheets = GetValue(Sheets, context, null);

            var options = new XlsxOptions
            {
                IncludeHeaders = IncludeHeaders,
                FreezeHeaderRow = FreezeHeaderRow,
                AutoFilter = AutoFilter,
                AutoSizeColumns = AutoSizeColumns,
                SplitLargeTables = SplitLargeTables
            };

            var dateFormat = GetValue(DateTimeFormat, context, null);
            if (!string.IsNullOrWhiteSpace(dateFormat))
            {
                options.DateTimeFormat = dateFormat;
            }

            if (table == null && string.IsNullOrWhiteSpace(sql) && (sheets == null || sheets.Count == 0))
            {
                throw new SQLiteActivityException(
                    "SQLite Export To Excel needs something to export: set 'SQL', 'Table to export', 'Data table' or 'Sheets'.");
            }

            var needsConnection = table == null || (sheets != null && sheets.Count > 0);
            var request = needsConnection ? ResolveConnection(context) : null;
            var rows = new int[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    if (table != null && (sheets == null || sheets.Count == 0))
                    {
                        rows[0] = XlsxWriter.Write(path, string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName, table, options, cancellationToken);
                        return path;
                    }

                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireReadLock(cancellationToken))
                    {
                        var handle = lease.Handle;
                        var queries = new List<KeyValuePair<string, string>>();

                        if (!string.IsNullOrWhiteSpace(sql))
                        {
                            queries.Add(new KeyValuePair<string, string>(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName, sql));
                        }

                        if (sheets != null)
                        {
                            foreach (var sheet in sheets)
                            {
                                queries.Add(new KeyValuePair<string, string>(sheet.Key, sheet.Value));
                            }
                        }

                        // The result sets are read one after another and streamed into the workbook, so
                        // only one reader is open at a time and memory stays flat.
                        rows[0] = handle.Execute((connection, transaction) =>
                        {
                            var commands = new List<System.Data.IDbCommand>();
                            var readers = new List<System.Data.IDataReader>();
                            var sheetList = new List<XlsxSheet>();

                            try
                            {
                                foreach (var query in queries)
                                {
                                    var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, query.Value, parameters, null, handle.Settings.CommandTimeoutSeconds);
                                    commands.Add(command);
                                    var reader = command.ExecuteReader();
                                    readers.Add(reader);
                                    sheetList.Add(new XlsxSheet(query.Key, reader));
                                }

                                if (table != null)
                                {
                                    sheetList.Insert(0, new XlsxSheet(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName, table));
                                }

                                return XlsxWriter.Write(path, sheetList, options, cancellationToken);
                            }
                            finally
                            {
                                foreach (var reader in readers)
                                {
                                    reader.Dispose();
                                }

                                foreach (var command in commands)
                                {
                                    command.Dispose();
                                }
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
