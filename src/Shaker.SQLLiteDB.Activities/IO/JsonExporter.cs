using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Shaker.SQLLiteDB.Activities.IO
{
    /// <summary>Serializes a <see cref="DataTable"/> to JSON without pulling in a JSON library.</summary>
    public static class JsonExporter
    {
        /// <summary>Writes a DataTable as a JSON array of objects and returns the number of rows.</summary>
        public static int Write(DataTable table, string path, bool indented, string dateTimeFormat, Encoding encoding, CancellationToken cancellationToken)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }

            var folder = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, encoding ?? new UTF8Encoding(false)))
            {
                return Write(table, writer, indented, dateTimeFormat, cancellationToken);
            }
        }

        /// <summary>Serializes a DataTable to any text writer.</summary>
        public static int Write(DataTable table, TextWriter writer, bool indented, string dateTimeFormat, CancellationToken cancellationToken)
        {
            var newLine = indented ? Environment.NewLine : string.Empty;
            var indent = indented ? "  " : string.Empty;
            var rows = 0;

            writer.Write('[');
            writer.Write(newLine);

            var first = true;

            foreach (DataRow row in table.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (row.RowState == DataRowState.Deleted)
                {
                    continue;
                }

                if (!first)
                {
                    writer.Write(',');
                    writer.Write(newLine);
                }

                first = false;
                writer.Write(indent);
                writer.Write('{');

                for (var i = 0; i < table.Columns.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(',');
                    }

                    if (indented)
                    {
                        writer.Write(newLine);
                        writer.Write(indent);
                        writer.Write(indent);
                    }

                    WriteString(writer, table.Columns[i].ColumnName);
                    writer.Write(':');
                    if (indented)
                    {
                        writer.Write(' ');
                    }

                    WriteValue(writer, row[i], dateTimeFormat);
                }

                if (indented)
                {
                    writer.Write(newLine);
                    writer.Write(indent);
                }

                writer.Write('}');
                rows++;
            }

            writer.Write(newLine);
            writer.Write(']');
            writer.Flush();
            return rows;
        }

        /// <summary>Serializes a DataTable to a JSON string.</summary>
        public static string ToJson(DataTable table, bool indented, string dateTimeFormat)
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                Write(table, writer, indented, dateTimeFormat, CancellationToken.None);
                return writer.ToString();
            }
        }

        private static void WriteValue(TextWriter writer, object value, string dateTimeFormat)
        {
            if (value == null || value == DBNull.Value)
            {
                writer.Write("null");
                return;
            }

            if (value is bool)
            {
                writer.Write(((bool)value) ? "true" : "false");
                return;
            }

            if (value is byte || value is sbyte || value is short || value is ushort ||
                value is int || value is uint || value is long || value is ulong)
            {
                writer.Write(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            if (value is float || value is double)
            {
                var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number))
                {
                    writer.Write("null");
                    return;
                }

                writer.Write(((IFormattable)value).ToString("R", CultureInfo.InvariantCulture));
                return;
            }

            if (value is decimal)
            {
                writer.Write(((decimal)value).ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (value is DateTime)
            {
                WriteString(writer, ((DateTime)value).ToString(
                    string.IsNullOrEmpty(dateTimeFormat) ? "o" : dateTimeFormat, CultureInfo.InvariantCulture));
                return;
            }

            if (value is byte[])
            {
                WriteString(writer, Convert.ToBase64String((byte[])value));
                return;
            }

            var formattable = value as IFormattable;
            WriteString(writer, formattable != null
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        internal static void WriteString(TextWriter writer, string value)
        {
            writer.Write('"');

            if (!string.IsNullOrEmpty(value))
            {
                foreach (var c in value)
                {
                    switch (c)
                    {
                        case '"':
                            writer.Write("\\\"");
                            break;
                        case '\\':
                            writer.Write("\\\\");
                            break;
                        case '\b':
                            writer.Write("\\b");
                            break;
                        case '\f':
                            writer.Write("\\f");
                            break;
                        case '\n':
                            writer.Write("\\n");
                            break;
                        case '\r':
                            writer.Write("\\r");
                            break;
                        case '\t':
                            writer.Write("\\t");
                            break;
                        default:
                            if (c < ' ' || (c >= '\u007F' && c <= '\u009F'))
                            {
                                writer.Write("\\u");
                                writer.Write(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                writer.Write(c);
                            }

                            break;
                    }
                }
            }

            writer.Write('"');
        }
    }
}
