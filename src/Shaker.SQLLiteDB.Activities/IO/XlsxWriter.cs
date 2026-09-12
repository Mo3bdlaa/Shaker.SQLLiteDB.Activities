using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Xml;

namespace Shaker.SQLLiteDB.Activities.IO
{
    /// <summary>One worksheet of an XLSX workbook, fed either from a DataTable or from a data reader.</summary>
    public class XlsxSheet
    {
        public XlsxSheet(string name, DataTable table)
        {
            Name = name;
            Table = table;
        }

        public XlsxSheet(string name, IDataReader reader)
        {
            Name = name;
            Reader = reader;
        }

        public string Name { get; set; }

        public DataTable Table { get; private set; }

        public IDataReader Reader { get; private set; }
    }

    /// <summary>
    /// Writes real .xlsx workbooks (SpreadsheetML inside a zip container) without any third party
    /// library, so the activity package stays free of Open XML version conflicts with other UiPath packages.
    /// Rows are streamed straight into the zip entry, so exports of millions of rows stay flat in memory.
    /// </summary>
    public static class XlsxWriter
    {
        private const string MainNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        private const int StyleDefault = 0;
        private const int StyleHeader = 1;
        private const int StyleDateTime = 2;
        private const int StyleDate = 3;

        /// <summary>Writes one worksheet.</summary>
        public static int Write(string path, string sheetName, DataTable table, XlsxOptions options, CancellationToken cancellationToken)
        {
            return Write(path, new List<XlsxSheet> { new XlsxSheet(sheetName, table) }, options, cancellationToken);
        }

        /// <summary>Writes a workbook with one or more worksheets and returns the number of data rows written.</summary>
        public static int Write(string path, IList<XlsxSheet> sheets, XlsxOptions options, CancellationToken cancellationToken)
        {
            if (sheets == null || sheets.Count == 0)
            {
                throw new ArgumentException("At least one worksheet is required.", "sheets");
            }

            if (options == null)
            {
                options = new XlsxOptions();
            }

            var folder = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var sheetNames = new List<string>();
            var totalRows = 0;

            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create, false))
            {
                var partIndex = 0;

                foreach (var sheet in sheets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Rows are streamed straight into the zip entry. Only a result that overflows the
                    // Excel row limit produces extra sheets, and those are written afterwards.
                    var overflow = new List<string>();
                    partIndex++;

                    var entry = archive.CreateEntry(SheetPartName(partIndex), CompressionLevel.Optimal);
                    using (var stream = entry.Open())
                    using (var writer = CreateXmlWriter(stream))
                    {
                        totalRows += WriteSheet(writer, sheet, options, sheetNames, overflow, cancellationToken);
                    }

                    foreach (var extra in overflow)
                    {
                        partIndex++;
                        WriteEntry(archive, SheetPartName(partIndex),
                            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" + extra);
                    }
                }

                WriteEntry(archive, "[Content_Types].xml", BuildContentTypes(sheetNames.Count));
                WriteEntry(archive, "_rels/.rels", BuildRootRelationships());
                WriteEntry(archive, "xl/workbook.xml", BuildWorkbook(sheetNames));
                WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationships(sheetNames.Count));
                WriteEntry(archive, "xl/styles.xml", BuildStyles(options));
            }

            return totalRows;
        }

        private static string SheetPartName(int index)
        {
            return "xl/worksheets/sheet" + index.ToString(CultureInfo.InvariantCulture) + ".xml";
        }

        private static XmlWriter CreateXmlWriter(Stream stream)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                CloseOutput = false,
                OmitXmlDeclaration = false
            };

            return XmlWriter.Create(stream, settings);
        }

        /// <summary>
        /// Writes one logical result set. When it does not fit into a single worksheet, the remaining rows
        /// are rendered into additional worksheets whose XML is returned through <paramref name="overflowSheets"/>.
        /// </summary>
        private static int WriteSheet(XmlWriter writer, XlsxSheet sheet, XlsxOptions options, List<string> sheetNames, List<string> overflowSheets, CancellationToken cancellationToken)
        {
            var baseName = SanitizeSheetName(sheet.Name, sheetNames);
            sheetNames.Add(baseName);

            var columnNames = GetColumnNames(sheet);
            if (columnNames.Count > XlsxOptions.ExcelMaxColumns)
            {
                throw new XlsxLimitException(string.Format(CultureInfo.InvariantCulture,
                    "The result has {0} columns; Excel supports at most {1}.", columnNames.Count, XlsxOptions.ExcelMaxColumns));
            }

            var rowsPerSheet = options.SplitLargeTables
                ? XlsxOptions.ExcelMaxRowsPerSheet - (options.IncludeHeaders ? 1 : 0)
                : int.MaxValue;

            // The <cols> element has to be written before any row, so column widths are estimated from
            // the header and from a bounded sample of the first rows instead of from the whole result.
            const int WidthSampleSize = 200;
            var sample = new List<object[]>(WidthSampleSize);
            var rows = EnumerateRows(sheet, cancellationToken);
            var enumerator = rows.GetEnumerator();

            try
            {
                while (sample.Count < WidthSampleSize && enumerator.MoveNext())
                {
                    sample.Add(enumerator.Current);
                }

                var widths = options.AutoSizeColumns ? EstimateColumnWidths(columnNames, sample) : null;

                var target = writer;
                StringWriter overflowBuffer = null;
                var written = 0;
                var totalRows = 0;

                StartSheet(target, columnNames, options, widths);

                foreach (var values in Concat(sample, enumerator))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (written >= rowsPerSheet)
                    {
                        if (!options.SplitLargeTables)
                        {
                            throw new XlsxLimitException(string.Format(CultureInfo.InvariantCulture,
                                "The result exceeds the Excel limit of {0} rows per worksheet. Switch 'SplitLargeTables' on or export to CSV instead.",
                                XlsxOptions.ExcelMaxRowsPerSheet));
                        }

                        EndSheet(target, columnNames.Count, written, options);

                        if (overflowBuffer != null)
                        {
                            target.Close();
                            overflowSheets.Add(overflowBuffer.ToString());
                        }

                        sheetNames.Add(SanitizeSheetName(baseName + " (" + (overflowSheets.Count + 2).ToString(CultureInfo.InvariantCulture) + ")", sheetNames));

                        overflowBuffer = new StringWriter(CultureInfo.InvariantCulture);
                        target = XmlWriter.Create(overflowBuffer, new XmlWriterSettings { Indent = false, OmitXmlDeclaration = true, CloseOutput = false });
                        StartSheet(target, columnNames, options, widths);
                        written = 0;
                    }

                    WriteRow(target, values, written + (options.IncludeHeaders ? 2 : 1), options);
                    written++;
                    totalRows++;
                }

                EndSheet(target, columnNames.Count, written, options);

                if (overflowBuffer != null)
                {
                    target.Close();
                    overflowSheets.Add(overflowBuffer.ToString());
                }

                return totalRows;
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        private static IEnumerable<object[]> Concat(IList<object[]> buffered, IEnumerator<object[]> rest)
        {
            foreach (var values in buffered)
            {
                yield return values;
            }

            while (rest.MoveNext())
            {
                yield return rest.Current;
            }
        }

        private static int[] EstimateColumnWidths(IList<string> columnNames, IList<object[]> sample)
        {
            var widths = new int[columnNames.Count];

            for (var i = 0; i < columnNames.Count; i++)
            {
                widths[i] = Math.Min(60, Math.Max(8, (columnNames[i] ?? string.Empty).Length + 4));
            }

            foreach (var values in sample)
            {
                for (var i = 0; i < widths.Length && i < values.Length; i++)
                {
                    var value = values[i];
                    if (value == null || value == DBNull.Value)
                    {
                        continue;
                    }

                    var length = value is DateTime ? 19 : Convert.ToString(value, CultureInfo.InvariantCulture).Length;
                    var candidate = Math.Min(60, length + 2);

                    if (candidate > widths[i])
                    {
                        widths[i] = candidate;
                    }
                }
            }

            return widths;
        }

        private static void StartSheet(XmlWriter writer, IList<string> columnNames, XlsxOptions options, int[] widths)
        {
            writer.WriteStartElement("worksheet", MainNamespace);
            writer.WriteAttributeString("xmlns", "r", null, RelationshipNamespace);

            if (options.IncludeHeaders && options.FreezeHeaderRow)
            {
                writer.WriteStartElement("sheetViews");
                writer.WriteStartElement("sheetView");
                writer.WriteAttributeString("workbookViewId", "0");
                writer.WriteStartElement("pane");
                writer.WriteAttributeString("ySplit", "1");
                writer.WriteAttributeString("topLeftCell", "A2");
                writer.WriteAttributeString("activePane", "bottomLeft");
                writer.WriteAttributeString("state", "frozen");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            if (widths != null && widths.Length > 0)
            {
                writer.WriteStartElement("cols");
                for (var i = 0; i < widths.Length; i++)
                {
                    writer.WriteStartElement("col");
                    writer.WriteAttributeString("min", (i + 1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("max", (i + 1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("width", widths[i].ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("customWidth", "1");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteStartElement("sheetData");

            if (options.IncludeHeaders)
            {
                writer.WriteStartElement("row");
                writer.WriteAttributeString("r", "1");

                for (var i = 0; i < columnNames.Count; i++)
                {
                    WriteInlineString(writer, CellReference(i, 1), columnNames[i], StyleHeader);
                }

                writer.WriteEndElement();
            }
        }

        private static void EndSheet(XmlWriter writer, int columnCount, int dataRows, XlsxOptions options)
        {
            writer.WriteEndElement(); // sheetData

            if (options.AutoFilter && options.IncludeHeaders && columnCount > 0)
            {
                var lastRow = dataRows + 1;
                writer.WriteStartElement("autoFilter");
                writer.WriteAttributeString("ref", "A1:" + ColumnName(columnCount - 1) + lastRow.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // worksheet
            writer.Flush();
        }

        private static void WriteRow(XmlWriter writer, object[] values, int rowNumber, XlsxOptions options)
        {
            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));

            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (value == null || value == DBNull.Value)
                {
                    continue;
                }

                var reference = CellReference(i, rowNumber);

                if (value is DateTime)
                {
                    var date = (DateTime)value;
                    var serial = ToExcelSerialDate(date);
                    if (serial.HasValue)
                    {
                        var style = date.TimeOfDay == TimeSpan.Zero ? StyleDate : StyleDateTime;
                        WriteNumber(writer, reference, serial.Value.ToString("R", CultureInfo.InvariantCulture), style);
                    }
                    else
                    {
                        WriteInlineString(writer, reference, date.ToString("O", CultureInfo.InvariantCulture), StyleDefault);
                    }

                    continue;
                }

                if (value is bool)
                {
                    writer.WriteStartElement("c");
                    writer.WriteAttributeString("r", reference);
                    writer.WriteAttributeString("t", "b");
                    writer.WriteElementString("v", ((bool)value) ? "1" : "0");
                    writer.WriteEndElement();
                    continue;
                }

                if (IsNumeric(value))
                {
                    WriteNumber(writer, reference, Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture), StyleDefault);
                    continue;
                }

                string text;
                if (value is byte[])
                {
                    text = Convert.ToBase64String((byte[])value);
                }
                else
                {
                    var formattable = value as IFormattable;
                    text = formattable != null
                        ? formattable.ToString(null, CultureInfo.InvariantCulture)
                        : Convert.ToString(value, CultureInfo.InvariantCulture);
                }

                WriteInlineString(writer, reference, text, StyleDefault);
            }

            writer.WriteEndElement();
        }

        private static void WriteNumber(XmlWriter writer, string reference, string value, int style)
        {
            writer.WriteStartElement("c");
            writer.WriteAttributeString("r", reference);
            if (style != StyleDefault)
            {
                writer.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteElementString("v", value);
            writer.WriteEndElement();
        }

        private static void WriteInlineString(XmlWriter writer, string reference, string value, int style)
        {
            writer.WriteStartElement("c");
            writer.WriteAttributeString("r", reference);
            writer.WriteAttributeString("t", "inlineStr");
            if (style != StyleDefault)
            {
                writer.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteStartElement("is");
            writer.WriteStartElement("t");
            writer.WriteAttributeString("xml", "space", null, "preserve");
            writer.WriteString(SanitizeXmlText(value));
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static bool IsNumeric(object value)
        {
            return value is byte || value is sbyte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong ||
                   value is float || value is double || value is decimal;
        }

        private static double? ToExcelSerialDate(DateTime value)
        {
            try
            {
                var serial = value.ToOADate();
                return serial < 0 ? (double?)null : serial;
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        /// <summary>Removes the control characters that are not allowed in XML content.</summary>
        internal static string SanitizeXmlText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            StringBuilder builder = null;

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                var valid = c == '\t' || c == '\n' || c == '\r' || (c >= ' ' && c <= '\uD7FF') ||
                            (c >= '\uE000' && c <= '\uFFFD');

                if (valid)
                {
                    if (builder != null)
                    {
                        builder.Append(c);
                    }

                    continue;
                }

                if (builder == null)
                {
                    builder = new StringBuilder(value.Length);
                    builder.Append(value, 0, i);
                }
            }

            return builder == null ? value : builder.ToString();
        }

        private static IEnumerable<object[]> EnumerateRows(XlsxSheet sheet, CancellationToken cancellationToken)
        {
            if (sheet.Table != null)
            {
                foreach (DataRow row in sheet.Table.Rows)
                {
                    if (row.RowState == DataRowState.Deleted)
                    {
                        continue;
                    }

                    yield return row.ItemArray;
                }

                yield break;
            }

            if (sheet.Reader != null)
            {
                var values = new object[sheet.Reader.FieldCount];
                while (sheet.Reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    sheet.Reader.GetValues(values);
                    var copy = new object[values.Length];
                    Array.Copy(values, copy, values.Length);
                    yield return copy;
                }
            }
        }

        private static List<string> GetColumnNames(XlsxSheet sheet)
        {
            var names = new List<string>();

            if (sheet.Table != null)
            {
                foreach (DataColumn column in sheet.Table.Columns)
                {
                    names.Add(column.ColumnName);
                }
            }
            else if (sheet.Reader != null)
            {
                for (var i = 0; i < sheet.Reader.FieldCount; i++)
                {
                    names.Add(sheet.Reader.GetName(i));
                }
            }

            return names;
        }

        /// <summary>Makes a sheet name Excel accepts: at most 31 characters, no []:*?/\ and unique in the workbook.</summary>
        internal static string SanitizeSheetName(string name, ICollection<string> existingNames)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Sheet";
            }

            var builder = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                builder.Append("[]:*?/\\".IndexOf(c) >= 0 ? '_' : c);
            }

            var candidate = builder.ToString().Trim('\'', ' ');
            if (candidate.Length == 0)
            {
                candidate = "Sheet";
            }

            if (candidate.Length > 31)
            {
                candidate = candidate.Substring(0, 31);
            }

            if (existingNames == null)
            {
                return candidate;
            }

            var unique = candidate;
            var suffix = 1;

            while (Contains(existingNames, unique))
            {
                suffix++;
                var tail = "_" + suffix.ToString(CultureInfo.InvariantCulture);
                unique = candidate.Length + tail.Length > 31
                    ? candidate.Substring(0, 31 - tail.Length) + tail
                    : candidate + tail;
            }

            return unique;
        }

        private static bool Contains(ICollection<string> names, string candidate)
        {
            foreach (var name in names)
            {
                if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Turns a zero based column index into A, B, … Z, AA, AB …</summary>
        internal static string ColumnName(int index)
        {
            var name = string.Empty;
            index++;

            while (index > 0)
            {
                var remainder = (index - 1) % 26;
                name = (char)('A' + remainder) + name;
                index = (index - 1) / 26;
            }

            return name;
        }

        private static string CellReference(int columnIndex, int rowNumber)
        {
            return ColumnName(columnIndex) + rowNumber.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static string BuildContentTypes(int sheetCount)
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            builder.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            builder.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            builder.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            builder.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");

            for (var i = 1; i <= sheetCount; i++)
            {
                builder.Append("<Override PartName=\"/xl/worksheets/sheet").Append(i.ToString(CultureInfo.InvariantCulture));
                builder.Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            }

            builder.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            builder.Append("</Types>");
            return builder.ToString();
        }

        private static string BuildRootRelationships()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                   "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                   "</Relationships>";
        }

        private static string BuildWorkbook(IList<string> sheetNames)
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            builder.Append("<workbook xmlns=\"").Append(MainNamespace).Append("\" xmlns:r=\"").Append(RelationshipNamespace).Append("\">");
            builder.Append("<sheets>");

            for (var i = 0; i < sheetNames.Count; i++)
            {
                builder.Append("<sheet name=\"").Append(EscapeXmlAttribute(sheetNames[i])).Append("\" sheetId=\"")
                       .Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append("\" r:id=\"rId")
                       .Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append("\"/>");
            }

            builder.Append("</sheets></workbook>");
            return builder.ToString();
        }

        private static string BuildWorkbookRelationships(int sheetCount)
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            builder.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");

            for (var i = 1; i <= sheetCount; i++)
            {
                builder.Append("<Relationship Id=\"rId").Append(i.ToString(CultureInfo.InvariantCulture));
                builder.Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet");
                builder.Append(i.ToString(CultureInfo.InvariantCulture)).Append(".xml\"/>");
            }

            builder.Append("<Relationship Id=\"rId").Append((sheetCount + 1).ToString(CultureInfo.InvariantCulture));
            builder.Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
            builder.Append("</Relationships>");
            return builder.ToString();
        }

        private static string BuildStyles(XlsxOptions options)
        {
            var dateTimeFormat = EscapeXmlAttribute(options.DateTimeFormat ?? "yyyy\\-mm\\-dd hh:mm:ss");
            var dateFormat = EscapeXmlAttribute(options.DateFormat ?? "yyyy\\-mm\\-dd");

            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            builder.Append("<styleSheet xmlns=\"").Append(MainNamespace).Append("\">");
            builder.Append("<numFmts count=\"2\">");
            builder.Append("<numFmt numFmtId=\"164\" formatCode=\"").Append(dateTimeFormat).Append("\"/>");
            builder.Append("<numFmt numFmtId=\"165\" formatCode=\"").Append(dateFormat).Append("\"/>");
            builder.Append("</numFmts>");
            builder.Append("<fonts count=\"2\">");
            builder.Append("<font><sz val=\"11\"/><name val=\"Calibri\"/></font>");
            builder.Append("<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font>");
            builder.Append("</fonts>");
            builder.Append("<fills count=\"3\">");
            builder.Append("<fill><patternFill patternType=\"none\"/></fill>");
            builder.Append("<fill><patternFill patternType=\"gray125\"/></fill>");
            builder.Append("<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFDDEBF7\"/><bgColor indexed=\"64\"/></patternFill></fill>");
            builder.Append("</fills>");
            builder.Append("<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>");
            builder.Append("<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");
            builder.Append("<cellXfs count=\"4\">");
            builder.Append("<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>");
            builder.Append("<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>");
            builder.Append("<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>");
            builder.Append("<xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>");
            builder.Append("</cellXfs>");
            builder.Append("<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>");
            builder.Append("</styleSheet>");
            return builder.ToString();
        }

        private static string EscapeXmlAttribute(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return SanitizeXmlText(value)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
