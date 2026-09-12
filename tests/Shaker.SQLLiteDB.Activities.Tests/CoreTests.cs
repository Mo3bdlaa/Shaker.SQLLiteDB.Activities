using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Shaker.SQLLiteDB.Activities.Core;
using Xunit;

namespace Shaker.SQLLiteDB.Activities.Tests
{
    public class CoreTests
    {
        private static readonly CancellationToken None = CancellationToken.None;

        [Fact]
        public void EmbeddedEngineLoadsWithoutAnyInstalledDriver()
        {
            Assert.False(string.IsNullOrWhiteSpace(SQLiteNative.EngineVersion));
            Assert.StartsWith("3.", SQLiteNative.EngineVersion);
        }

        [Fact]
        public void OpeningCreatesTheDatabaseAndTurnsOnWal()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            Assert.True(handle.IsOpen);
            Assert.True(System.IO.File.Exists(db.Path));

            var mode = handle.Execute((connection, transaction) =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = "pragma journal_mode;";
                return Convert.ToString(command.ExecuteScalar());
            }, None);

            Assert.Equal("wal", mode, ignoreCase: true);
        }

        [Fact]
        public void MissingFolderIsCreated()
        {
            using var db = new TestDatabase();
            var nested = System.IO.Path.Combine(db.Folder, "a", "b", "nested.db");

            using var handle = SQLiteConnectionHandle.Open(new SQLiteConnectionSettings { DatabasePath = nested });
            Assert.True(System.IO.File.Exists(nested));
        }

        [Fact]
        public void NamedAndPositionalParametersBothWork()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            Execute(handle, "create table t (id integer primary key, name text, amount real);");

            handle.Execute((connection, transaction) =>
            {
                using var command = SQLiteCommandExecutor.CreateCommand(connection, transaction,
                    "insert into t (id, name, amount) values (@id, @name, @amount);",
                    new Dictionary<string, object> { { "id", 1 }, { "@name", "first" }, { "amount", 1.5 } }, null, 30);
                return command.ExecuteNonQuery();
            }, None);

            handle.Execute((connection, transaction) =>
            {
                using var command = SQLiteCommandExecutor.CreateCommand(connection, transaction,
                    "insert into t (id, name, amount) values (?, ?, ?);", null, new object[] { 2, "second", 2.5 }, 30);
                return command.ExecuteNonQuery();
            }, None);

            var table = SQLiteCommandExecutor.ExecuteQuery(handle, "select id, name, amount from t order by id;",
                null, null, SQLiteColumnTyping.Auto, 0, "Result", None);

            Assert.Equal(2, table.Rows.Count);
            Assert.Equal("first", table.Rows[0]["name"]);
            Assert.Equal(2.5, Convert.ToDouble(table.Rows[1]["amount"]));
        }

        [Fact]
        public void PlaceholderRewriteLeavesLiteralsAlone()
        {
            var rewritten = SQLiteCommandExecutor.RewritePositionalPlaceholders(
                "select '? literal', \"od?d\" from t where a = ? and b = ? -- ? comment", 2);

            Assert.Contains("'? literal'", rewritten);
            Assert.Contains("\"od?d\"", rewritten);
            Assert.Contains("a = @p1", rewritten);
            Assert.Contains("b = @p2", rewritten);
            Assert.Contains("-- ? comment", rewritten);
        }

        [Fact]
        public void PlaceholderCountMismatchIsReported()
        {
            Assert.Throws<SQLiteActivityException>(() =>
                SQLiteCommandExecutor.RewritePositionalPlaceholders("select * from t where a = ? and b = ?;", 1));
        }

        [Fact]
        public void ColumnTypesAreDerivedFromTheValues()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            Execute(handle, "create table t (a, b, c);");
            Execute(handle, "insert into t values (1, 'text', 1.5), (2, 'more', 2.5);");

            var auto = SQLiteCommandExecutor.ExecuteQuery(handle, "select * from t;", null, null, SQLiteColumnTyping.Auto, 0, null, None);
            Assert.Equal(typeof(long), auto.Columns["a"].DataType);
            Assert.Equal(typeof(string), auto.Columns["b"].DataType);
            Assert.Equal(typeof(double), auto.Columns["c"].DataType);

            var text = SQLiteCommandExecutor.ExecuteQuery(handle, "select * from t;", null, null, SQLiteColumnTyping.AllText, 0, null, None);
            Assert.Equal(typeof(string), text.Columns["a"].DataType);
            Assert.Equal("1", text.Rows[0]["a"]);
        }

        [Fact]
        public void MixedColumnFallsBackToObject()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            Execute(handle, "create table t (v);");
            Execute(handle, "insert into t values (1), ('text');");

            var table = SQLiteCommandExecutor.ExecuteQuery(handle, "select v from t;", null, null, SQLiteColumnTyping.Auto, 0, null, None);
            Assert.Equal(typeof(object), table.Columns["v"].DataType);
            Assert.Equal(2, table.Rows.Count);
        }

        [Fact]
        public void DuplicateColumnNamesAreMadeUnique()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            Execute(handle, "create table t (id integer);");
            Execute(handle, "insert into t values (1);");

            var table = SQLiteCommandExecutor.ExecuteQuery(handle, "select id, id from t;", null, null, SQLiteColumnTyping.Auto, 0, null, None);
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal("id", table.Columns[0].ColumnName);
            Assert.Equal("id_2", table.Columns[1].ColumnName);
        }

        [Fact]
        public void MaxRowsStopsReadingEarly()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            Execute(handle, "create table t (id integer);");
            Execute(handle, "insert into t values (1),(2),(3),(4),(5);");

            var table = SQLiteCommandExecutor.ExecuteQuery(handle, "select id from t order by id;", null, null, SQLiteColumnTyping.Auto, 3, null, None);
            Assert.Equal(3, table.Rows.Count);
        }

        [Fact]
        public void TransactionRollbackUndoesTheWork()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            Execute(handle, "create table t (id integer);");

            var transaction = handle.BeginTransaction(SQLiteTransactionMode.Immediate);
            Execute(handle, "insert into t values (1);");
            transaction.Rollback();

            Assert.Equal(0L, Scalar(handle, "select count(*) from t;"));
        }

        [Fact]
        public void NestedScopeUsesASavepoint()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            Execute(handle, "create table t (id integer);");

            var outer = handle.BeginTransaction(SQLiteTransactionMode.Immediate);
            Execute(handle, "insert into t values (1);");

            var inner = handle.BeginTransaction(SQLiteTransactionMode.Immediate);
            Assert.True(inner.IsSavepoint);
            Execute(handle, "insert into t values (2);");
            inner.Rollback();

            outer.Commit();

            Assert.Equal(1L, Scalar(handle, "select count(*) from t;"));
            Assert.Equal(1L, Scalar(handle, "select id from t;"));
        }

        [Fact]
        public void SchemaHelpersDescribeTheDatabase()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            Execute(handle, "create table people (id integer primary key, name text not null);");
            Execute(handle, "create view people_view as select * from people;");

            Assert.True(SQLiteSchema.TableExists(handle, "people", None));
            Assert.False(SQLiteSchema.TableExists(handle, "missing", None));

            var tables = SQLiteSchema.GetTableNames(handle, false, false, None);
            Assert.Contains("people", tables);
            Assert.DoesNotContain("people_view", tables);

            var withViews = SQLiteSchema.GetTableNames(handle, true, false, None);
            Assert.Contains("people_view", withViews);

            var columns = SQLiteSchema.GetColumns(handle, "people", None);
            Assert.Equal(2, columns.Count);
            Assert.True(columns[0].IsPrimaryKey);
            Assert.True(columns[1].NotNull);
        }

        [Fact]
        public void IdentifiersWithQuotesAreEscaped()
        {
            Assert.Equal("\"odd\"\"name\"", SQLiteSchema.Quote("odd\"name"));
            Assert.Equal("\"main\".\"t\"", SQLiteSchema.QualifiedName("main.t"));
        }

        [Fact]
        public void CreateTableStatementMatchesTheDataTable()
        {
            var table = new DataTable("people");
            table.Columns.Add("id", typeof(long));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("amount", typeof(decimal));
            table.Columns.Add("photo", typeof(byte[]));
            table.Columns.Add("created", typeof(DateTime));

            var sql = SQLiteSchema.BuildCreateTable("people", table, true, new[] { "id" });

            Assert.Contains("CREATE TABLE IF NOT EXISTS \"people\"", sql);
            Assert.Contains("\"id\" INTEGER", sql);
            Assert.Contains("\"name\" TEXT", sql);
            Assert.Contains("\"amount\" NUMERIC", sql);
            Assert.Contains("\"photo\" BLOB", sql);
            Assert.Contains("PRIMARY KEY (\"id\")", sql);
        }

        [Fact]
        public void ReadOnlyConnectionRefusesToWrite()
        {
            using var db = new TestDatabase();

            using (var writer = db.Open())
            {
                Execute(writer, "create table t (id integer);");
            }

            var settings = db.Settings();
            settings.OpenMode = SQLiteOpenMode.ReadOnly;

            using var reader = SQLiteConnectionHandle.Open(settings);
            Assert.Equal(0L, Scalar(reader, "select count(*) from t;"));
            Assert.ThrowsAny<Exception>(() => Execute(reader, "insert into t values (1);"));
        }

        internal static void Execute(SQLiteConnectionHandle handle, string sql)
        {
            handle.Execute((connection, transaction) =>
            {
                using var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, sql, null, null, 30);
                return command.ExecuteNonQuery();
            }, None);
        }

        internal static object Scalar(SQLiteConnectionHandle handle, string sql)
        {
            return handle.Execute((connection, transaction) =>
            {
                using var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, sql, null, null, 30);
                return command.ExecuteScalar();
            }, None);
        }
    }
}
