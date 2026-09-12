using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>Options for <see cref="SQLiteBulkWriter"/>.</summary>
    public class SQLiteBulkOptions
    {
        /// <summary>How constraint violations are handled. Use <see cref="SQLiteConflictPolicy.Upsert"/> together with <see cref="KeyColumns"/> for insert-or-update.</summary>
        public SQLiteConflictPolicy ConflictPolicy { get; set; } = SQLiteConflictPolicy.Abort;

        /// <summary>Columns that identify a row for an UPSERT. They must be covered by a unique index or primary key.</summary>
        public IList<string> KeyColumns { get; set; }

        /// <summary>Columns updated on conflict. Empty means "every column that is not a key column".</summary>
        public IList<string> UpdateColumns { get; set; }

        /// <summary>Rows per transaction. Zero puts every row in a single transaction. Default 1000.</summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>Maps DataTable column names onto table column names.</summary>
        public IDictionary<string, string> ColumnMapping { get; set; }

        /// <summary>Create the destination table from the shape of the DataTable when it does not exist.</summary>
        public bool CreateTableIfNotExists { get; set; }

        /// <summary>Silently ignore DataTable columns that the destination table does not have.</summary>
        public bool IgnoreExtraColumns { get; set; } = true;
    }

    /// <summary>Result of a bulk write.</summary>
    public class SQLiteBulkResult
    {
        /// <summary>Rows that the engine reported as inserted or updated.</summary>
        public int AffectedRows { get; set; }

        /// <summary>Rows that were sent to the database.</summary>
        public int ProcessedRows { get; set; }

        /// <summary>Rows skipped because of <see cref="SQLiteConflictPolicy.Ignore"/>.</summary>
        public int SkippedRows { get; set; }

        /// <summary>Number of transactions that were committed.</summary>
        public int Batches { get; set; }
    }

    /// <summary>
    /// Writes a whole <see cref="DataTable"/> into a SQLite table using one prepared statement and
    /// batched transactions. This is an order of magnitude faster than sending one INSERT per row.
    /// </summary>
    public static class SQLiteBulkWriter
    {
        public static SQLiteBulkResult Write(
            SQLiteConnectionHandle handle,
            string tableName,
            DataTable data,
            SQLiteBulkOptions options,
            CancellationToken cancellationToken)
        {
            if (handle == null)
            {
                throw new ArgumentNullException("handle");
            }

            if (data == null)
            {
                throw new SQLiteActivityException("The DataTable to write is null.");
            }

            if (options == null)
            {
                options = new SQLiteBulkOptions();
            }

            var result = new SQLiteBulkResult();
            if (data.Rows.Count == 0)
            {
                return result;
            }

            if (options.CreateTableIfNotExists && !SQLiteSchema.TableExists(handle, tableName, cancellationToken))
            {
                var createSql = SQLiteSchema.BuildCreateTable(tableName, data, true, options.KeyColumns);
                handle.Execute((connection, transaction) =>
                {
                    using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, createSql, null, null, handle.Settings.CommandTimeoutSeconds))
                    {
                        return command.ExecuteNonQuery();
                    }
                }, cancellationToken);
            }

            var destinationColumns = SQLiteSchema.GetColumns(handle, tableName, cancellationToken);
            var mapping = BuildMapping(data, destinationColumns, options);

            if (mapping.Count == 0)
            {
                throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                    "None of the DataTable columns match a column of table '{0}'. Check the column names or use 'ColumnMapping'.", tableName));
            }

            var sql = BuildStatement(tableName, mapping, options);
            var batchSize = options.BatchSize <= 0 ? int.MaxValue : options.BatchSize;

            var rows = new List<DataRow>(data.Rows.Count);
            foreach (DataRow row in data.Rows)
            {
                if (row.RowState != DataRowState.Deleted)
                {
                    rows.Add(row);
                }
            }

            // Cursor kept outside the retried delegate: after a transient lock error the work resumes
            // at the first row that was not committed yet, so no row is ever written twice.
            var committedRows = 0;
            var joinsCallerTransaction = handle.CurrentTransaction != null && handle.CurrentTransaction.IsActive;

            // Replaying half of an insert inside somebody else's transaction is not safe, so in that
            // case the error is handed to the surrounding transaction scope instead of being retried.
            var retry = joinsCallerTransaction ? new SQLiteRetryOptions { MaxAttempts = 1 } : null;

            handle.Execute<object>((connection, transaction) =>
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.Transaction = transaction;

                    if (handle.Settings.CommandTimeoutSeconds > 0)
                    {
                        command.CommandTimeout = handle.Settings.CommandTimeoutSeconds;
                    }

                    var parameters = new SqliteParameter[mapping.Count];
                    for (var i = 0; i < mapping.Count; i++)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@p" + i.ToString(CultureInfo.InvariantCulture);
                        command.Parameters.Add(parameter);
                        parameters[i] = parameter;
                    }

                    command.Prepare();

                    var ownsTransaction = transaction == null;
                    SqliteTransaction batchTransaction = null;
                    var batchAffected = 0;
                    var batchProcessed = 0;
                    var batchSkipped = 0;

                    try
                    {
                        var index = committedRows;
                        while (index < rows.Count)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (ownsTransaction && batchTransaction == null)
                            {
                                batchTransaction = connection.BeginTransaction(IsolationLevel.Serializable, false);
                                command.Transaction = batchTransaction;
                            }

                            var row = rows[index];
                            for (var i = 0; i < mapping.Count; i++)
                            {
                                parameters[i].Value = SQLiteCommandExecutor.NormalizeValue(row[mapping[i].SourceColumn]);
                            }

                            var affected = command.ExecuteNonQuery();
                            batchAffected += affected;
                            batchProcessed++;

                            if (affected == 0)
                            {
                                batchSkipped++;
                            }

                            index++;

                            if (batchProcessed >= batchSize || index == rows.Count)
                            {
                                if (ownsTransaction)
                                {
                                    batchTransaction.Commit();
                                    batchTransaction.Dispose();
                                    batchTransaction = null;
                                    command.Transaction = null;
                                }

                                result.AffectedRows += batchAffected;
                                result.ProcessedRows += batchProcessed;
                                result.SkippedRows += batchSkipped;
                                result.Batches++;
                                committedRows = index;
                                batchAffected = 0;
                                batchProcessed = 0;
                                batchSkipped = 0;
                            }
                        }
                    }
                    catch
                    {
                        if (batchTransaction != null)
                        {
                            try
                            {
                                batchTransaction.Rollback();
                            }
                            catch
                            {
                                // The original error is the interesting one.
                            }

                            batchTransaction.Dispose();
                        }

                        throw;
                    }
                }

                return null;
            }, cancellationToken, retry);

            return result;
        }

        private class ColumnMap
        {
            public string SourceColumn { get; set; }
            public string DestinationColumn { get; set; }
        }

        private static List<ColumnMap> BuildMapping(DataTable data, List<SQLiteColumnInfo> destinationColumns, SQLiteBulkOptions options)
        {
            var destinationByName = new Dictionary<string, SQLiteColumnInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in destinationColumns)
            {
                destinationByName[column.Name] = column;
            }

            var mapping = new List<ColumnMap>();

            foreach (DataColumn column in data.Columns)
            {
                var destinationName = column.ColumnName;

                if (options.ColumnMapping != null)
                {
                    string mapped;
                    if (options.ColumnMapping.TryGetValue(column.ColumnName, out mapped))
                    {
                        if (string.IsNullOrWhiteSpace(mapped))
                        {
                            continue;
                        }

                        destinationName = mapped;
                    }
                }

                SQLiteColumnInfo destination;
                if (!destinationByName.TryGetValue(destinationName, out destination))
                {
                    if (options.IgnoreExtraColumns)
                    {
                        continue;
                    }

                    throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                        "The destination table has no column named '{0}'. Set 'IgnoreExtraColumns' or supply a column mapping.", destinationName));
                }

                mapping.Add(new ColumnMap { SourceColumn = column.ColumnName, DestinationColumn = destination.Name });
            }

            return mapping;
        }

        private static string BuildStatement(string tableName, List<ColumnMap> mapping, SQLiteBulkOptions options)
        {
            var builder = new StringBuilder();
            builder.Append("INSERT ");

            switch (options.ConflictPolicy)
            {
                case SQLiteConflictPolicy.Ignore:
                    builder.Append("OR IGNORE ");
                    break;
                case SQLiteConflictPolicy.Replace:
                    builder.Append("OR REPLACE ");
                    break;
                case SQLiteConflictPolicy.Rollback:
                    builder.Append("OR ROLLBACK ");
                    break;
            }

            builder.Append("INTO ").Append(SQLiteSchema.QualifiedName(tableName)).Append(" (");

            for (var i = 0; i < mapping.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(SQLiteSchema.Quote(mapping[i].DestinationColumn));
            }

            builder.Append(") VALUES (");

            for (var i = 0; i < mapping.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append("@p").Append(i.ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(')');

            if (options.ConflictPolicy == SQLiteConflictPolicy.Upsert)
            {
                if (options.KeyColumns == null || options.KeyColumns.Count == 0)
                {
                    throw new SQLiteActivityException(
                        "An UPSERT needs 'KeyColumns': the columns that decide whether a row already exists. They must be covered by a primary key or a unique index.");
                }

                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var quotedKeys = new List<string>();
                foreach (var key in options.KeyColumns)
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    keys.Add(key.Trim());
                    quotedKeys.Add(SQLiteSchema.Quote(key.Trim()));
                }

                var updates = new List<string>();
                if (options.UpdateColumns != null && options.UpdateColumns.Count > 0)
                {
                    foreach (var column in options.UpdateColumns)
                    {
                        if (!string.IsNullOrWhiteSpace(column))
                        {
                            var quoted = SQLiteSchema.Quote(column.Trim());
                            updates.Add(quoted + " = excluded." + quoted);
                        }
                    }
                }
                else
                {
                    foreach (var map in mapping)
                    {
                        if (keys.Contains(map.DestinationColumn))
                        {
                            continue;
                        }

                        var quoted = SQLiteSchema.Quote(map.DestinationColumn);
                        updates.Add(quoted + " = excluded." + quoted);
                    }
                }

                builder.Append(" ON CONFLICT (").Append(string.Join(", ", quotedKeys.ToArray())).Append(") DO ");
                builder.Append(updates.Count == 0
                    ? "NOTHING"
                    : "UPDATE SET " + string.Join(", ", updates.ToArray()));
            }

            builder.Append(';');
            return builder.ToString();
        }
    }
}
