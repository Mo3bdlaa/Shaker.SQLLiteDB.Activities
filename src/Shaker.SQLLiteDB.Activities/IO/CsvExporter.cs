using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Shaker.SQLLiteDB.Activities.IO
{
    /// <summary>Writes a <see cref="DataTable"/> or a data reader to a CSV file, streaming row by row.</summary>
    public static class CsvExporter
    {
        /// <summary>Writes a DataTable to a CSV file and returns the number of data rows written.</summary>
        public static int Write(DataTable table, string path, CsvFormat format, bool append, CancellationToken cancellationToken)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }

            var names = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                names.Add(column.ColumnName);
            }

            var appendToExisting = append && File.Exists(path) && new FileInfo(path).Length > 0;

            using (var writer = CreateWriter(path, format, append))
            {
                var rowsWritten = 0;
                WriteHeader(writer, names, format, appendToExisting);

                foreach (DataRow row in table.Rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (row.RowState == DataRowState.Deleted)
                    {
                        continue;
                    }

                    WriteRow(writer, row.ItemArray, format);
                    rowsWritten++;
                }

                writer.Flush();
                return rowsWritten;
            }
        }

        /// <summary>Streams a data reader to a CSV file. Nothing is buffered, so result sets of any size are fine.</summary>
        public static int Write(IDataReader reader, string path, CsvFormat format, bool append, CancellationToken cancellationToken)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            var appendToExisting = append && File.Exists(path) && new FileInfo(path).Length > 0;

            using (var writer = CreateWriter(path, format, append))
            {
                var names = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    names.Add(reader.GetName(i));
                }

                WriteHeader(writer, names, format, appendToExisting);

                var values = new object[reader.FieldCount];
                var rowsWritten = 0;

                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    reader.GetValues(values);
                    WriteRow(writer, values, format);
                    rowsWritten++;
                }

                writer.Flush();
                return rowsWritten;
            }
        }

        private static StreamWriter CreateWriter(string path, CsvFormat format, bool append)
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var encoding = format.Encoding ?? new UTF8Encoding(true);
            var stream = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
            return new StreamWriter(stream, encoding) { NewLine = format.NewLine ?? "\r\n" };
        }

        private static void WriteHeader(TextWriter writer, IList<string> names, CsvFormat format, bool appendingToNonEmptyFile)
        {
            if (!format.IncludeHeaders || appendingToNonEmptyFile)
            {
                return;
            }

            var line = new StringBuilder();
            for (var i = 0; i < names.Count; i++)
            {
                if (i > 0)
                {
                    line.Append(format.Delimiter);
                }

                line.Append(Escape(names[i], format));
            }

            writer.Write(line.ToString());
            writer.Write(format.NewLine ?? "\r\n");
        }

        private static void WriteRow(TextWriter writer, object[] values, CsvFormat format)
        {
            var culture = format.GetCulture();
            var line = new StringBuilder();

            for (var i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    line.Append(format.Delimiter);
                }

                line.Append(Escape(FormatValue(values[i], format, culture), format));
            }

            writer.Write(line.ToString());
            writer.Write(format.NewLine ?? "\r\n");
        }

        internal static string FormatValue(object value, CsvFormat format, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value)
            {
                return format.NullText ?? string.Empty;
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString(format.DateTimeFormat ?? "yyyy-MM-dd HH:mm:ss", culture);
            }

            if (value is byte[])
            {
                return Convert.ToBase64String((byte[])value);
            }

            if (value is bool)
            {
                return ((bool)value) ? "True" : "False";
            }

            var formattable = value as IFormattable;
            return formattable != null ? formattable.ToString(null, culture) : Convert.ToString(value, culture);
        }

        internal static string Escape(string value, CsvFormat format)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            var quote = format.QuoteCharacter;
            var delimiter = format.Delimiter ?? ",";
            var mustQuote = format.QuoteAllFields ||
                            value.IndexOf(quote) >= 0 ||
                            value.IndexOf('\n') >= 0 ||
                            value.IndexOf('\r') >= 0 ||
                            (delimiter.Length > 0 && value.IndexOf(delimiter, StringComparison.Ordinal) >= 0) ||
                            value.StartsWith(" ", StringComparison.Ordinal) ||
                            value.EndsWith(" ", StringComparison.Ordinal);

            if (!mustQuote)
            {
                return value;
            }

            return quote + value.Replace(quote.ToString(), new string(quote, 2)) + quote;
        }
    }
}
