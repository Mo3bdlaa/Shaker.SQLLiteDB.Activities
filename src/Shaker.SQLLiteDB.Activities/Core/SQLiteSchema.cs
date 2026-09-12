using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>One column of a SQLite table.</summary>
    public class SQLiteColumnInfo
    {
        public int Ordinal { get; set; }
        public string Name { get; set; }
        public string DeclaredType { get; set; }
        public bool NotNull { get; set; }
        public string DefaultValue { get; set; }
        public bool IsPrimaryKey { get; set; }

        public override string ToString()
        {
            return Name + " " + DeclaredType;
        }
    }

    /// <summary>Schema helpers: quoting, lookups and CREATE TABLE generation.</summary>
    public static class SQLiteSchema
    {
        /// <summary>Quotes an identifier so that reserved words and spaces are safe to use.</summary>
        public static string Quote(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new SQLiteActivityException("An empty table or column name was supplied.");
            }

            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Quotes a possibly schema qualified name such as <c>main.Customers</c>.</summary>
        public static string QualifiedName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new SQLiteActivityException("The table name is empty.");
            }

            var trimmed = tableName.Trim();
            if (trimmed.StartsWith("\"", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                return trimmed;
            }

            var separator = trimmed.IndexOf('.');
            if (separator > 0 && separator < trimmed.Length - 1)
            {
                return Quote(trimmed.Substring(0, separator)) + "." + Quote(trimmed.Substring(separator + 1));
            }

            return Quote(trimmed);
        }

        /// <summary>True when the table (or view) exists.</summary>
        public static bool TableExists(SQLiteConnectionHandle handle, string tableName, CancellationToken cancellationToken)
        {
            return handle.Execute((connection, transaction) =>
            {
                using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction,
                    "select count(1) from sqlite_master where type in ('table','view') and lower(name) = lower(@name);",
                    new Dictionary<string, object> { { "@name", StripSchema(tableName) } }, null, handle.Settings.CommandTimeoutSeconds))
                {
                    var result = command.ExecuteScalar();
                    return result != null && result != DBNull.Value && Convert.ToInt64(result, CultureInfo.InvariantCulture) > 0;
                }
            }, cancellationToken);
        }

        /// <summary>Lists the tables, and optionally the views, of the database.</summary>
        public static List<string> GetTableNames(SQLiteConnectionHandle handle, bool includeViews, bool includeSystemTables, CancellationToken cancellationToken)
        {
            var types = includeViews ? "('table','view')" : "('table')";
            var filter = includeSystemTables ? string.Empty : " and name not like 'sqlite\\_%' escape '\\'";

            return handle.Execute((connection, transaction) =>
            {
                var names = new List<string>();
                using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction,
                    "select name from sqlite_master where type in " + types + filter + " order by name;",
                    null, null, handle.Settings.CommandTimeoutSeconds))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        names.Add(reader.GetString(0));
                    }
                }

                return names;
            }, cancellationToken);
        }

        /// <summary>Reads the column definitions of a table.</summary>
        public static List<SQLiteColumnInfo> GetColumns(SQLiteConnectionHandle handle, string tableName, CancellationToken cancellationToken)
        {
            return handle.Execute((connection, transaction) =>
            {
                var columns = new List<SQLiteColumnInfo>();
                using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction,
                    "pragma table_info(" + QualifiedName(tableName) + ");", null, null, handle.Settings.CommandTimeoutSeconds))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(new SQLiteColumnInfo
                        {
                            Ordinal = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                            Name = reader.IsDBNull(1) ? null : Convert.ToString(reader.GetValue(1)),
                            DeclaredType = reader.IsDBNull(2) ? string.Empty : Convert.ToString(reader.GetValue(2)),
                            NotNull = !reader.IsDBNull(3) && Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture) != 0,
                            DefaultValue = reader.IsDBNull(4) ? null : Convert.ToString(reader.GetValue(4)),
                            IsPrimaryKey = !reader.IsDBNull(5) && Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture) != 0
                        });
                    }
                }

                if (columns.Count == 0)
                {
                    throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                        "Table '{0}' does not exist or has no columns.", tableName));
                }

                return columns;
            }, cancellationToken);
        }

        /// <summary>Returns the column definitions of a table as a DataTable, ready to be shown in a workflow.</summary>
        public static DataTable GetSchemaTable(SQLiteConnectionHandle handle, string tableName, CancellationToken cancellationToken)
        {
            var columns = GetColumns(handle, tableName, cancellationToken);
            var table = new DataTable(tableName + "_Schema");
            table.Locale = CultureInfo.InvariantCulture;
            table.Columns.Add("Ordinal", typeof(int));
            table.Columns.Add("ColumnName", typeof(string));
            table.Columns.Add("DeclaredType", typeof(string));
            table.Columns.Add("NotNull", typeof(bool));
            table.Columns.Add("DefaultValue", typeof(string));
            table.Columns.Add("IsPrimaryKey", typeof(bool));

            foreach (var column in columns)
            {
                table.Rows.Add(column.Ordinal, column.Name, column.DeclaredType, column.NotNull, column.DefaultValue, column.IsPrimaryKey);
            }

            table.AcceptChanges();
            return table;
        }

        /// <summary>Maps a .NET type onto a SQLite storage class.</summary>
        public static string MapClrType(Type type)
        {
            if (type == null)
            {
                return "TEXT";
            }

            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(bool) || underlying == typeof(byte) || underlying == typeof(sbyte) ||
                underlying == typeof(short) || underlying == typeof(ushort) || underlying == typeof(int) ||
                underlying == typeof(uint) || underlying == typeof(long) || underlying == typeof(ulong))
            {
                return "INTEGER";
            }

            if (underlying == typeof(float) || underlying == typeof(double))
            {
                return "REAL";
            }

            if (underlying == typeof(decimal))
            {
                return "NUMERIC";
            }

            if (underlying == typeof(byte[]))
            {
                return "BLOB";
            }

            return "TEXT";
        }

        /// <summary>Builds a CREATE TABLE statement that matches the shape of a DataTable.</summary>
        public static string BuildCreateTable(string tableName, DataTable table, bool ifNotExists, IEnumerable<string> primaryKeyColumns)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }

            if (table.Columns.Count == 0)
            {
                throw new SQLiteActivityException("The DataTable has no columns, so no table can be created from it.");
            }

            var keys = new List<string>();
            if (primaryKeyColumns != null)
            {
                foreach (var key in primaryKeyColumns)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        keys.Add(key.Trim());
                    }
                }
            }

            if (keys.Count == 0 && table.PrimaryKey != null)
            {
                foreach (var column in table.PrimaryKey)
                {
                    keys.Add(column.ColumnName);
                }
            }

            var builder = new StringBuilder();
            builder.Append("CREATE TABLE ");
            if (ifNotExists)
            {
                builder.Append("IF NOT EXISTS ");
            }

            builder.Append(QualifiedName(tableName)).AppendLine(" (");

            for (var i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];
                builder.Append("  ").Append(Quote(column.ColumnName)).Append(' ').Append(MapClrType(column.DataType));

                if (!column.AllowDBNull)
                {
                    builder.Append(" NOT NULL");
                }

                if (i < table.Columns.Count - 1 || keys.Count > 0)
                {
                    builder.Append(',');
                }

                builder.AppendLine();
            }

            if (keys.Count > 0)
            {
                var quoted = new List<string>();
                foreach (var key in keys)
                {
                    quoted.Add(Quote(key));
                }

                builder.Append("  PRIMARY KEY (").Append(string.Join(", ", quoted.ToArray())).AppendLine(")");
            }

            builder.Append(");");
            return builder.ToString();
        }

        private static string StripSchema(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return tableName;
            }

            var trimmed = tableName.Trim().Trim('"', '[', ']', '`');
            var separator = trimmed.LastIndexOf('.');
            return separator >= 0 && separator < trimmed.Length - 1 ? trimmed.Substring(separator + 1) : trimmed;
        }
    }
}
