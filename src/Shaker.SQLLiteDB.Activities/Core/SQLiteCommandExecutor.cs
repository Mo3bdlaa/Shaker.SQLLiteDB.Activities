using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>
    /// Builds and runs SQLite commands: parameter binding, positional placeholders and conversion of a
    /// result set into a <see cref="DataTable"/>.
    /// </summary>
    public static class SQLiteCommandExecutor
    {
        /// <summary>Creates a command with the given SQL, parameters and timeout.</summary>
        public static SqliteCommand CreateCommand(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string sql,
            IDictionary<string, object> namedParameters,
            IEnumerable positionalParameters,
            int commandTimeoutSeconds)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new SQLiteActivityException("The SQL statement is empty.");
            }

            var command = connection.CreateCommand();

            try
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;

                if (commandTimeoutSeconds > 0)
                {
                    command.CommandTimeout = commandTimeoutSeconds;
                }

                var positional = ToList(positionalParameters);
                command.CommandText = positional.Count > 0 ? RewritePositionalPlaceholders(sql, positional.Count) : sql;

                for (var i = 0; i < positional.Count; i++)
                {
                    AddParameter(command, "@p" + (i + 1).ToString(CultureInfo.InvariantCulture), positional[i]);
                }

                if (namedParameters != null)
                {
                    foreach (var parameter in namedParameters)
                    {
                        AddParameter(command, NormalizeParameterName(parameter.Key), parameter.Value);
                    }
                }

                return command;
            }
            catch
            {
                command.Dispose();
                throw;
            }
        }

        /// <summary>Adds one parameter, converting the value to something SQLite understands.</summary>
        public static SqliteParameter AddParameter(SqliteCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = NormalizeValue(value);
            command.Parameters.Add(parameter);
            return parameter;
        }

        /// <summary>Maps a .NET value onto a value the SQLite provider can bind.</summary>
        public static object NormalizeValue(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return DBNull.Value;
            }

            if (value is char)
            {
                return value.ToString();
            }

            if (value is Enum)
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }

            if (value is DateTimeOffset)
            {
                return ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture);
            }

            if (value is Uri)
            {
                return value.ToString();
            }

            return value;
        }

        /// <summary>Accepts <c>name</c>, <c>@name</c>, <c>$name</c> and <c>:name</c> and normalizes it to <c>@name</c>.</summary>
        public static string NormalizeParameterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new SQLiteActivityException("A query parameter was given without a name.");
            }

            var trimmed = name.Trim();
            var first = trimmed[0];
            return first == '@' || first == '$' || first == ':' ? trimmed : "@" + trimmed;
        }

        /// <summary>
        /// Replaces the <c>?</c> placeholders of a statement with <c>@p1 … @pN</c>, leaving string
        /// literals, quoted identifiers and comments untouched.
        /// </summary>
        public static string RewritePositionalPlaceholders(string sql, int parameterCount)
        {
            if (sql.IndexOf('?') < 0)
            {
                return sql;
            }

            var builder = new StringBuilder(sql.Length + parameterCount * 3);
            var index = 0;
            var ordinal = 0;

            while (index < sql.Length)
            {
                var c = sql[index];

                switch (c)
                {
                    case '\'':
                    case '"':
                    case '`':
                        index = CopyQuoted(sql, index, c, c, builder);
                        continue;
                    case '[':
                        index = CopyQuoted(sql, index, '[', ']', builder);
                        continue;
                    case '-':
                        if (index + 1 < sql.Length && sql[index + 1] == '-')
                        {
                            index = CopyLineComment(sql, index, builder);
                            continue;
                        }

                        break;
                    case '/':
                        if (index + 1 < sql.Length && sql[index + 1] == '*')
                        {
                            index = CopyBlockComment(sql, index, builder);
                            continue;
                        }

                        break;
                    case '?':
                        ordinal++;
                        builder.Append("@p").Append(ordinal.ToString(CultureInfo.InvariantCulture));
                        index++;
                        continue;
                }

                builder.Append(c);
                index++;
            }

            if (ordinal > 0 && ordinal != parameterCount)
            {
                throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                    "The statement contains {0} '?' placeholders but {1} parameters were supplied.", ordinal, parameterCount));
            }

            return builder.ToString();
        }

        private static int CopyQuoted(string sql, int start, char open, char close, StringBuilder builder)
        {
            builder.Append(sql[start]);
            var index = start + 1;

            while (index < sql.Length)
            {
                var c = sql[index];
                builder.Append(c);
                index++;

                if (c == close)
                {
                    // Doubled quote inside the literal: keep going.
                    if (open == close && index < sql.Length && sql[index] == close)
                    {
                        builder.Append(sql[index]);
                        index++;
                        continue;
                    }

                    break;
                }
            }

            return index;
        }

        private static int CopyLineComment(string sql, int start, StringBuilder builder)
        {
            var index = start;
            while (index < sql.Length && sql[index] != '\n')
            {
                builder.Append(sql[index]);
                index++;
            }

            return index;
        }

        private static int CopyBlockComment(string sql, int start, StringBuilder builder)
        {
            var index = start;
            while (index < sql.Length)
            {
                if (sql[index] == '*' && index + 1 < sql.Length && sql[index + 1] == '/')
                {
                    builder.Append("*/");
                    return index + 2;
                }

                builder.Append(sql[index]);
                index++;
            }

            return index;
        }

        /// <summary>Runs a query and returns the result as a <see cref="DataTable"/>.</summary>
        public static DataTable ExecuteQuery(
            SQLiteConnectionHandle handle,
            string sql,
            IDictionary<string, object> namedParameters,
            IEnumerable positionalParameters,
            SQLiteColumnTyping typing,
            int maxRows,
            string tableName,
            CancellationToken cancellationToken)
        {
            return handle.Execute((connection, transaction) =>
            {
                using (var command = CreateCommand(connection, transaction, sql, namedParameters, positionalParameters, handle.Settings.CommandTimeoutSeconds))
                using (var reader = command.ExecuteReader())
                {
                    return Fill(reader, typing, maxRows, tableName, cancellationToken);
                }
            }, cancellationToken);
        }

        /// <summary>Materializes a data reader into a <see cref="DataTable"/>.</summary>
        public static DataTable Fill(IDataReader reader, SQLiteColumnTyping typing, int maxRows, string tableName, CancellationToken cancellationToken)
        {
            var table = new DataTable(string.IsNullOrWhiteSpace(tableName) ? "Table" : tableName);
            table.Locale = CultureInfo.InvariantCulture;

            var fieldCount = reader.FieldCount;
            if (fieldCount == 0)
            {
                return table;
            }

            var names = new string[fieldCount];
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < fieldCount; i++)
            {
                var name = reader.GetName(i);
                if (string.IsNullOrWhiteSpace(name))
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

                names[i] = candidate;
            }

            var rows = new List<object[]>();
            var observedTypes = new HashSet<Type>[fieldCount];

            for (var i = 0; i < fieldCount; i++)
            {
                observedTypes[i] = new HashSet<Type>();
            }

            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var values = new object[fieldCount];
                reader.GetValues(values);

                for (var i = 0; i < fieldCount; i++)
                {
                    var value = values[i];
                    if (value == null || value == DBNull.Value)
                    {
                        values[i] = DBNull.Value;
                    }
                    else
                    {
                        observedTypes[i].Add(value.GetType());
                    }
                }

                rows.Add(values);

                if (maxRows > 0 && rows.Count >= maxRows)
                {
                    break;
                }
            }

            for (var i = 0; i < fieldCount; i++)
            {
                table.Columns.Add(names[i], ResolveColumnType(reader, i, observedTypes[i], typing));
            }

            table.BeginLoadData();
            try
            {
                foreach (var values in rows)
                {
                    if (typing == SQLiteColumnTyping.AllText)
                    {
                        for (var i = 0; i < fieldCount; i++)
                        {
                            values[i] = values[i] == DBNull.Value ? DBNull.Value : (object)FormatAsText(values[i]);
                        }
                    }

                    table.Rows.Add(values);
                }
            }
            finally
            {
                table.EndLoadData();
            }

            table.AcceptChanges();
            return table;
        }

        private static Type ResolveColumnType(IDataReader reader, int ordinal, HashSet<Type> observedTypes, SQLiteColumnTyping typing)
        {
            if (typing == SQLiteColumnTyping.AllText)
            {
                return typeof(string);
            }

            if (typing == SQLiteColumnTyping.Declared)
            {
                try
                {
                    var declared = reader.GetFieldType(ordinal);
                    return declared ?? typeof(object);
                }
                catch
                {
                    return typeof(object);
                }
            }

            // Auto: SQLite is dynamically typed, so trust what actually came back.
            if (observedTypes.Count == 1)
            {
                foreach (var type in observedTypes)
                {
                    return type;
                }
            }

            if (observedTypes.Count == 0)
            {
                try
                {
                    var declared = reader.GetFieldType(ordinal);
                    return declared ?? typeof(object);
                }
                catch
                {
                    return typeof(object);
                }
            }

            return typeof(object);
        }

        private static string FormatAsText(object value)
        {
            if (value is byte[])
            {
                return Convert.ToBase64String((byte[])value);
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            }

            var formattable = value as IFormattable;
            return formattable != null
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static IList<object> ToList(IEnumerable values)
        {
            var result = new List<object>();
            if (values == null)
            {
                return result;
            }

            if (values is string)
            {
                result.Add(values);
                return result;
            }

            foreach (var value in values)
            {
                result.Add(value);
            }

            return result;
        }
    }
}
