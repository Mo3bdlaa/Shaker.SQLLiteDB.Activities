using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Shaker.SQLLiteDB.Activities.IO
{
    /// <summary>Reads a CSV file into a <see cref="DataTable"/>. Handles quoted fields and embedded line breaks.</summary>
    public static class CsvImporter
    {
        /// <summary>Reads a CSV file. All columns come back as text unless <paramref name="inferTypes"/> is set.</summary>
        public static DataTable Read(string path, CsvFormat format, bool inferTypes, int maxRows, CancellationToken cancellationToken)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The CSV file to import does not exist: " + path, path);
            }

            if (format == null)
            {
                format = new CsvFormat();
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, format.Encoding ?? Encoding.UTF8, true))
            {
                return Read(reader, format, inferTypes, maxRows, Path.GetFileNameWithoutExtension(path), cancellationToken);
            }
        }

        /// <summary>Reads CSV content from any text reader.</summary>
        public static DataTable Read(TextReader reader, CsvFormat format, bool inferTypes, int maxRows, string tableName, CancellationToken cancellationToken)
        {
            var table = new DataTable(string.IsNullOrWhiteSpace(tableName) ? "Csv" : tableName);
            table.Locale = CultureInfo.InvariantCulture;

            var lookahead = new LookaheadReader(reader);
            var records = new List<List<string>>();
            List<string> record;
            var headerTaken = false;
            var columnCount = 0;

            while ((record = ReadRecord(lookahead, format)) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!headerTaken && format.IncludeHeaders)
                {
                    headerTaken = true;
                    columnCount = record.Count;
                    AddColumns(table, record, true);
                    continue;
                }

                if (record.Count > columnCount)
                {
                    columnCount = record.Count;
                }

                records.Add(record);

                if (maxRows > 0 && records.Count >= maxRows)
                {
                    break;
                }
            }

            if (table.Columns.Count == 0)
            {
                var generated = new List<string>();
                for (var i = 0; i < columnCount; i++)
                {
                    generated.Add("Column" + (i + 1).ToString(CultureInfo.InvariantCulture));
                }

                AddColumns(table, generated, false);
            }

            while (table.Columns.Count < columnCount)
            {
                table.Columns.Add("Column" + (table.Columns.Count + 1).ToString(CultureInfo.InvariantCulture), typeof(string));
            }

            table.BeginLoadData();
            try
            {
                foreach (var values in records)
                {
                    var row = table.NewRow();
                    for (var i = 0; i < table.Columns.Count; i++)
                    {
                        row[i] = i < values.Count ? (object)values[i] : DBNull.Value;
                    }

                    table.Rows.Add(row);
                }
            }
            finally
            {
                table.EndLoadData();
            }

            if (inferTypes)
            {
                table = ConvertColumnTypes(table, format.GetCulture());
            }

            table.AcceptChanges();
            return table;
        }

        private static void AddColumns(DataTable table, IList<string> names, bool fromHeader)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < names.Count; i++)
            {
                var name = fromHeader ? (names[i] ?? string.Empty).Trim() : names[i];
                if (string.IsNullOrEmpty(name))
                {
                    name = "Column" + (i + 1).ToString(CultureInfo.InvariantCulture);
                }

                var candidate = name;
                var suffix = 1;
                while (!used.Add(candidate))
                {
                    suffix++;
                    candidate = name + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                }

                table.Columns.Add(candidate, typeof(string));
            }
        }

        private static DataTable ConvertColumnTypes(DataTable source, CultureInfo culture)
        {
            var types = new Type[source.Columns.Count];

            for (var i = 0; i < source.Columns.Count; i++)
            {
                var allLong = true;
                var allDouble = true;
                var allDate = true;
                var allBool = true;
                var anyValue = false;

                foreach (DataRow row in source.Rows)
                {
                    var text = row[i] as string;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    anyValue = true;
                    long longValue;
                    double doubleValue;
                    DateTime dateValue;
                    bool boolValue;

                    allLong &= long.TryParse(text, NumberStyles.Integer, culture, out longValue);
                    allDouble &= double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out doubleValue);
                    allDate &= DateTime.TryParse(text, culture, DateTimeStyles.None, out dateValue);
                    allBool &= bool.TryParse(text, out boolValue);

                    if (!allLong && !allDouble && !allDate && !allBool)
                    {
                        break;
                    }
                }

                if (!anyValue)
                {
                    types[i] = typeof(string);
                }
                else if (allBool)
                {
                    types[i] = typeof(bool);
                }
                else if (allLong)
                {
                    types[i] = typeof(long);
                }
                else if (allDouble)
                {
                    types[i] = typeof(double);
                }
                else if (allDate)
                {
                    types[i] = typeof(DateTime);
                }
                else
                {
                    types[i] = typeof(string);
                }
            }

            var typed = new DataTable(source.TableName);
            typed.Locale = CultureInfo.InvariantCulture;

            for (var i = 0; i < source.Columns.Count; i++)
            {
                typed.Columns.Add(source.Columns[i].ColumnName, types[i]);
            }

            typed.BeginLoadData();
            try
            {
                foreach (DataRow row in source.Rows)
                {
                    var values = new object[source.Columns.Count];
                    for (var i = 0; i < source.Columns.Count; i++)
                    {
                        var text = row[i] as string;
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            values[i] = DBNull.Value;
                            continue;
                        }

                        try
                        {
                            values[i] = Convert.ChangeType(text, types[i], culture);
                        }
                        catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                        {
                            values[i] = DBNull.Value;
                        }
                    }

                    typed.Rows.Add(values);
                }
            }
            finally
            {
                typed.EndLoadData();
            }

            return typed;
        }

        /// <summary>
        /// Text reader with a small push back buffer. It lets the parser look ahead for a multi character
        /// delimiter and put the characters back when only a part of it matched.
        /// </summary>
        private sealed class LookaheadReader
        {
            private readonly TextReader _inner;
            private readonly Stack<int> _pushedBack = new Stack<int>();

            public LookaheadReader(TextReader inner)
            {
                _inner = inner;
            }

            public int Read()
            {
                return _pushedBack.Count > 0 ? _pushedBack.Pop() : _inner.Read();
            }

            public int Peek()
            {
                return _pushedBack.Count > 0 ? _pushedBack.Peek() : _inner.Peek();
            }

            public void PushBack(IList<int> characters)
            {
                for (var i = characters.Count - 1; i >= 0; i--)
                {
                    _pushedBack.Push(characters[i]);
                }
            }
        }

        /// <summary>Reads one CSV record, following quoting rules and allowing line breaks inside quoted fields.</summary>
        private static List<string> ReadRecord(LookaheadReader reader, CsvFormat format)
        {
            var delimiter = string.IsNullOrEmpty(format.Delimiter) ? "," : format.Delimiter;
            var quote = format.QuoteCharacter;

            if (reader.Peek() < 0)
            {
                return null;
            }

            var fields = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var fieldHadContent = false;

            while (true)
            {
                var next = reader.Read();

                if (next < 0)
                {
                    if (field.Length > 0 || fields.Count > 0 || fieldHadContent)
                    {
                        fields.Add(field.ToString());
                        return fields;
                    }

                    return fields.Count > 0 ? fields : null;
                }

                var c = (char)next;

                if (inQuotes)
                {
                    if (c == quote)
                    {
                        if (reader.Peek() == quote)
                        {
                            field.Append(quote);
                            reader.Read();
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                if (c == quote && field.Length == 0)
                {
                    inQuotes = true;
                    fieldHadContent = true;
                    continue;
                }

                if (c == '\r')
                {
                    if (reader.Peek() == '\n')
                    {
                        reader.Read();
                    }

                    fields.Add(field.ToString());
                    return fields;
                }

                if (c == '\n')
                {
                    fields.Add(field.ToString());
                    return fields;
                }

                if (c == delimiter[0] && MatchesDelimiter(reader, delimiter))
                {
                    fields.Add(field.ToString());
                    field.Length = 0;
                    fieldHadContent = false;
                    continue;
                }

                field.Append(c);
            }
        }

        private static bool MatchesDelimiter(LookaheadReader reader, string delimiter)
        {
            if (delimiter.Length == 1)
            {
                return true;
            }

            // Multi character delimiter: the first character was already consumed, so try to match the
            // rest and put everything back when it turns out not to be a delimiter after all.
            var consumed = new List<int>();

            for (var i = 1; i < delimiter.Length; i++)
            {
                if (reader.Peek() != delimiter[i])
                {
                    reader.PushBack(consumed);
                    return false;
                }

                consumed.Add(reader.Read());
            }

            return true;
        }
    }
}
