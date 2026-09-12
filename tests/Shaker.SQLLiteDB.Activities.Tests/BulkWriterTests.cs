using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Shaker.SQLLiteDB.Activities.Core;
using Xunit;

namespace Shaker.SQLLiteDB.Activities.Tests
{
    public class BulkWriterTests
    {
        private static readonly CancellationToken None = CancellationToken.None;

        private static DataTable People(params object[] rows)
        {
            var table = new DataTable("people");
            table.Columns.Add("id", typeof(long));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("score", typeof(double));

            for (var i = 0; i < rows.Length; i += 3)
            {
                table.Rows.Add(rows[i], rows[i + 1], rows[i + 2]);
            }

            return table;
        }

        [Fact]
        public void RowsAreWrittenInBatches()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key, name text, score real);");

            var table = new DataTable("people");
            table.Columns.Add("id", typeof(long));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("score", typeof(double));

            for (var i = 1; i <= 250; i++)
            {
                table.Rows.Add(i, "person " + i, i * 1.5);
            }

            var result = SQLiteBulkWriter.Write(handle, "people", table, new SQLiteBulkOptions { BatchSize = 100 }, None);

            Assert.Equal(250, result.AffectedRows);
            Assert.Equal(250, result.ProcessedRows);
            Assert.Equal(3, result.Batches);
            Assert.Equal(250L, Convert.ToInt64(CoreTests.Scalar(handle, "select count(*) from people;")));
        }

        [Fact]
        public void ConflictsCanBeIgnored()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key, name text, score real);");
            CoreTests.Execute(handle, "insert into people values (1, 'existing', 1.0);");

            var result = SQLiteBulkWriter.Write(handle, "people", People(1L, "new", 2.0, 2L, "second", 3.0),
                new SQLiteBulkOptions { ConflictPolicy = SQLiteConflictPolicy.Ignore }, None);

            Assert.Equal(1, result.AffectedRows);
            Assert.Equal(1, result.SkippedRows);
            Assert.Equal("existing", CoreTests.Scalar(handle, "select name from people where id = 1;"));
        }

        [Fact]
        public void ConflictsCanReplaceTheExistingRow()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key, name text, score real);");
            CoreTests.Execute(handle, "insert into people values (1, 'existing', 1.0);");

            SQLiteBulkWriter.Write(handle, "people", People(1L, "replaced", 9.0),
                new SQLiteBulkOptions { ConflictPolicy = SQLiteConflictPolicy.Replace }, None);

            Assert.Equal("replaced", CoreTests.Scalar(handle, "select name from people where id = 1;"));
        }

        [Fact]
        public void UpsertUpdatesTheRowThatIsAlreadyThere()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key, name text, score real);");
            CoreTests.Execute(handle, "insert into people values (1, 'existing', 1.0);");

            SQLiteBulkWriter.Write(handle, "people", People(1L, "updated", 5.0, 2L, "fresh", 6.0),
                new SQLiteBulkOptions { ConflictPolicy = SQLiteConflictPolicy.Upsert, KeyColumns = new[] { "id" } }, None);

            Assert.Equal("updated", CoreTests.Scalar(handle, "select name from people where id = 1;"));
            Assert.Equal("fresh", CoreTests.Scalar(handle, "select name from people where id = 2;"));
            Assert.Equal(2L, Convert.ToInt64(CoreTests.Scalar(handle, "select count(*) from people;")));
        }

        [Fact]
        public void UpsertCanRestrictTheColumnsItUpdates()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key, name text, score real);");
            CoreTests.Execute(handle, "insert into people values (1, 'existing', 1.0);");

            SQLiteBulkWriter.Write(handle, "people", People(1L, "updated", 5.0),
                new SQLiteBulkOptions
                {
                    ConflictPolicy = SQLiteConflictPolicy.Upsert,
                    KeyColumns = new[] { "id" },
                    UpdateColumns = new[] { "score" }
                }, None);

            Assert.Equal("existing", CoreTests.Scalar(handle, "select name from people where id = 1;"));
            Assert.Equal(5.0, Convert.ToDouble(CoreTests.Scalar(handle, "select score from people where id = 1;")));
        }

        [Fact]
        public void UpsertWithoutKeyColumnsIsRejectedWithAClearMessage()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key, name text, score real);");

            var error = Assert.Throws<SQLiteActivityException>(() => SQLiteBulkWriter.Write(handle, "people",
                People(1L, "x", 1.0), new SQLiteBulkOptions { ConflictPolicy = SQLiteConflictPolicy.Upsert }, None));

            Assert.Contains("KeyColumns", error.Message);
        }

        [Fact]
        public void TheTableCanBeCreatedFromTheDataTable()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            var result = SQLiteBulkWriter.Write(handle, "people", People(1L, "first", 1.0),
                new SQLiteBulkOptions { CreateTableIfNotExists = true, KeyColumns = new[] { "id" } }, None);

            Assert.Equal(1, result.AffectedRows);
            Assert.True(SQLiteSchema.TableExists(handle, "people", None));
        }

        [Fact]
        public void ColumnsCanBeMappedAndExtraOnesIgnored()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key, full_name text);");

            var table = new DataTable();
            table.Columns.Add("id", typeof(long));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("ignored", typeof(string));
            table.Rows.Add(1L, "Sara", "not a column");

            SQLiteBulkWriter.Write(handle, "people", table, new SQLiteBulkOptions
            {
                ColumnMapping = new Dictionary<string, string> { { "name", "full_name" } },
                IgnoreExtraColumns = true
            }, None);

            Assert.Equal("Sara", CoreTests.Scalar(handle, "select full_name from people where id = 1;"));
        }

        [Fact]
        public void AnUnknownColumnIsReportedWhenItIsNotIgnored()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key);");

            var table = new DataTable();
            table.Columns.Add("id", typeof(long));
            table.Columns.Add("nope", typeof(string));
            table.Rows.Add(1L, "x");

            var error = Assert.Throws<SQLiteActivityException>(() => SQLiteBulkWriter.Write(handle, "people", table,
                new SQLiteBulkOptions { IgnoreExtraColumns = false }, None));

            Assert.Contains("nope", error.Message);
        }

        [Fact]
        public void NullsAndBlobsSurviveTheRoundTrip()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table items (id integer primary key, note text, payload blob, at text);");

            var table = new DataTable();
            table.Columns.Add("id", typeof(long));
            table.Columns.Add("note", typeof(string));
            table.Columns.Add("payload", typeof(byte[]));
            table.Columns.Add("at", typeof(DateTime));
            table.Rows.Add(1L, DBNull.Value, new byte[] { 1, 2, 3 }, new DateTime(2024, 5, 6, 7, 8, 9));

            SQLiteBulkWriter.Write(handle, "items", table, new SQLiteBulkOptions(), None);

            var read = SQLiteCommandExecutor.ExecuteQuery(handle, "select note, payload, at from items;", null, null,
                SQLiteColumnTyping.Auto, 0, null, None);

            Assert.Equal(DBNull.Value, read.Rows[0]["note"]);
            Assert.Equal(new byte[] { 1, 2, 3 }, (byte[])read.Rows[0]["payload"]);
            Assert.Contains("2024-05-06", Convert.ToString(read.Rows[0]["at"], System.Globalization.CultureInfo.InvariantCulture));
        }

        [Fact]
        public void AFailingBatchLeavesTheDatabaseUnchanged()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key, name text not null, score real);");

            var table = People(1L, "ok", 1.0, 2L, null, 2.0);

            Assert.ThrowsAny<Exception>(() => SQLiteBulkWriter.Write(handle, "people", table, new SQLiteBulkOptions { BatchSize = 0 }, None));
            Assert.Equal(0L, Convert.ToInt64(CoreTests.Scalar(handle, "select count(*) from people;")));
        }

        [Fact]
        public void AnEmptyDataTableIsANoOp()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table people (id integer primary key, name text, score real);");

            var result = SQLiteBulkWriter.Write(handle, "people", People(), new SQLiteBulkOptions(), None);
            Assert.Equal(0, result.ProcessedRows);
        }
    }
}
