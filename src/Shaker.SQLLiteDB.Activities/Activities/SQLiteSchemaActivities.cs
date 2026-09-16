using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Schema
{
    /// <summary>Checks whether a table or a view exists.</summary>
    [Category("SQLite.Schema")]
    [DisplayName("SQLite Table Exists")]
    [Description("Returns true when the given table or view exists in the database.")]
    public class SQLiteTableExists : SQLiteActivityBase<bool>
    {
        public SQLiteTableExists()
        {
            DisplayName = "SQLite Table Exists";
        }

        [Category("Input")]
        [DisplayName("Table name")]
        [Description("Name of the table or view to look for.")]
        [RequiredArgument]
        public InArgument<string> TableName { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var tableName = TableName.Get(context);

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    {
                        return SQLiteSchema.TableExists(lease.Handle, tableName, cancellationToken);
                    }
                }
            };
        }
    }

    /// <summary>Lists the tables of a database.</summary>
    [Category("SQLite.Schema")]
    [DisplayName("SQLite Get Table Names")]
    [Description("Returns the names of the tables, and optionally the views, of the database.")]
    public class SQLiteGetTableNames : SQLiteActivityBase<List<string>>
    {
        public SQLiteGetTableNames()
        {
            DisplayName = "SQLite Get Table Names";
        }

        [Category("Options")]
        [DisplayName("Include views")]
        [Description("Also list views, not only tables.")]
        public bool IncludeViews { get; set; }

        [Category("Options")]
        [DisplayName("Include internal tables")]
        [Description("Also list the internal sqlite_ tables.")]
        public bool IncludeSystemTables { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var includeViews = IncludeViews;
            var includeSystem = IncludeSystemTables;

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    {
                        return SQLiteSchema.GetTableNames(lease.Handle, includeViews, includeSystem, cancellationToken);
                    }
                }
            };
        }
    }

    /// <summary>Returns the columns of a table with their types and constraints.</summary>
    [Category("SQLite.Schema")]
    [DisplayName("SQLite Get Table Schema")]
    [Description("Returns the columns of a table as a DataTable: ordinal, name, declared type, not null, default value and primary key flag.")]
    public class SQLiteGetTableSchema : SQLiteActivityBase<DataTable>
    {
        public SQLiteGetTableSchema()
        {
            DisplayName = "SQLite Get Table Schema";
        }

        [Category("Input")]
        [DisplayName("Table name")]
        [Description("Table whose columns should be described.")]
        [RequiredArgument]
        public InArgument<string> TableName { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var tableName = TableName.Get(context);

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    {
                        return SQLiteSchema.GetSchemaTable(lease.Handle, tableName, cancellationToken);
                    }
                }
            };
        }
    }

    /// <summary>Creates a table whose columns match a DataTable.</summary>
    [Category("SQLite.Schema")]
    [DisplayName("SQLite Create Table")]
    [Description("Creates a table from the shape of a DataTable, mapping .NET types onto SQLite storage classes.")]
    public class SQLiteCreateTable : SQLiteActivityBase<bool>
    {
        public SQLiteCreateTable()
        {
            DisplayName = "SQLite Create Table";
        }

        [Category("Input")]
        [DisplayName("Table name")]
        [Description("Name of the table to create.")]
        [RequiredArgument]
        public InArgument<string> TableName { get; set; }

        [Category("Input")]
        [DisplayName("Data table")]
        [Description("DataTable whose columns describe the table to create.")]
        [RequiredArgument]
        public InArgument<DataTable> DataTable { get; set; }

        [Category("Options")]
        [DisplayName("Primary key columns")]
        [Description("Columns that form the primary key. Empty uses the primary key of the DataTable, if it has one.")]
        public InArgument<IEnumerable<string>> PrimaryKeyColumns { get; set; }

        [Category("Options")]
        [DisplayName("Skip when it exists")]
        [Description("Use CREATE TABLE IF NOT EXISTS, so running the workflow twice is harmless.")]
        public bool IfNotExists { get; set; } = true;

        [Category("Output")]
        [DisplayName("Statement")]
        [Description("The CREATE TABLE statement that was executed.")]
        public OutArgument<string> Statement { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var tableName = TableName.Get(context);
            var data = DataTable.Get(context);
            var keys = GetValue(PrimaryKeyColumns, context, null);
            var ifNotExists = IfNotExists;
            var statement = new string[1];

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    var sql = SQLiteSchema.BuildCreateTable(tableName, data, ifNotExists, keys);
                    statement[0] = sql;

                    using (var lease = request.Acquire())
                    using (lease.Handle.AcquireWriteLock(cancellationToken))
                    {
                        var existed = SQLiteSchema.TableExists(lease.Handle, tableName, cancellationToken);

                        lease.Handle.Execute((connection, transaction) =>
                        {
                            using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, sql, null, null, lease.Handle.Settings.CommandTimeoutSeconds))
                            {
                                return command.ExecuteNonQuery();
                            }
                        }, cancellationToken);

                        return !existed;
                    }
                },
                ApplyOutputs = (activityContext, created) => SetValue(Statement, activityContext, statement[0])
            };
        }
    }
}
