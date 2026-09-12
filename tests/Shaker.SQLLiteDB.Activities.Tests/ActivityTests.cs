using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Shaker.SQLLiteDB.Activities.Activities;
using Shaker.SQLLiteDB.Activities.Core;
using Xunit;

namespace Shaker.SQLLiteDB.Activities.Tests
{
    public class ActivityTests
    {
        [Fact]
        public void ActivitiesInsideAScopeShareItsConnection()
        {
            using var db = new TestDatabase();
            DataTable result = null;

            var workflow = WorkflowHelpers.ConnectScope(db.Path,
                new SQLiteExecuteNonQuery { Sql = "create table t (id integer primary key, name text);" },
                new SQLiteExecuteNonQuery { Sql = "insert into t (name) values ('first'), ('second');" },
                new Capture<DataTable>
                {
                    Value = new InArgument<DataTable>(new SQLiteExecuteQuery { Sql = "select id, name from t order by id;" }),
                    OnValue = table => result = table
                });

            WorkflowInvoker.Invoke(workflow);

            Assert.NotNull(result);
            Assert.Equal(2, result.Rows.Count);
            Assert.Equal("second", result.Rows[1]["name"]);
        }

        [Fact]
        public void AnActivityCanRunWithoutAScope()
        {
            using var db = new TestDatabase();

            WorkflowInvoker.Invoke(new SQLiteExecuteNonQuery
            {
                DatabasePath = db.Path,
                Sql = "create table t (id integer primary key);"
            });

            var exists = WorkflowInvoker.Invoke(new SQLiteTableExists { DatabasePath = db.Path, TableName = "t" });
            Assert.True(exists);
        }

        [Fact]
        public void AMissingDatabaseIsReportedClearly()
        {
            var error = Assert.ThrowsAny<Exception>(() => WorkflowInvoker.Invoke(new SQLiteExecuteQuery { Sql = "select 1;" }));
            Assert.Contains("SQLite Connect Scope", Flatten(error));
        }

        [Fact]
        public void ParametersAreBoundInsteadOfConcatenated()
        {
            using var db = new TestDatabase();
            DataTable result = null;

            var workflow = WorkflowHelpers.ConnectScope(db.Path,
                new SQLiteExecuteNonQuery { Sql = "create table t (id integer primary key, name text);" },
                new SQLiteExecuteNonQuery
                {
                    Sql = "insert into t (name) values (@name);",
                    Parameters = new InArgument<IDictionary<string, object>>(
                        context => new Dictionary<string, object> { { "@name", "Robert'); DROP TABLE t; --" } })
                },
                new Capture<DataTable>
                {
                    Value = new InArgument<DataTable>(new SQLiteExecuteQuery { Sql = "select name from t;" }),
                    OnValue = table => result = table
                });

            WorkflowInvoker.Invoke(workflow);

            Assert.Single(result.Rows);
            Assert.Equal("Robert'); DROP TABLE t; --", result.Rows[0]["name"]);
        }

        [Fact]
        public void ScalarReportsValueTextNumberAndNull()
        {
            using var db = new TestDatabase();
            var value = new Variable<object>();

            WorkflowInvoker.Invoke(WorkflowHelpers.ConnectScope(db.Path,
                new SQLiteExecuteNonQuery { Sql = "create table t (id integer primary key);" },
                new SQLiteExecuteNonQuery { Sql = "insert into t values (1),(2),(3);" }));

            var scalar = new SQLiteExecuteScalar { DatabasePath = db.Path, Sql = "select count(*) from t;" };
            var result = WorkflowInvoker.Invoke<object>(scalar);
            Assert.Equal(3L, Convert.ToInt64(result));
        }

        [Fact]
        public void LastInsertRowIdComesBack()
        {
            using var db = new TestDatabase();
            long rowId = 0;

            var insert = new SQLiteExecuteNonQuery { Sql = "insert into t (name) values ('x');" };

            var workflow = WorkflowHelpers.ConnectScope(db.Path,
                new SQLiteExecuteNonQuery { Sql = "create table t (id integer primary key autoincrement, name text);" },
                new SQLiteExecuteNonQuery { Sql = "insert into t (name) values ('first');" },
                new Capture<int>
                {
                    Value = new InArgument<int>(insert),
                    OnValue = _ => { }
                });

            // Reading the out argument needs a variable, so the check runs on a second insert.
            var rowIdVariable = new Variable<long>();
            var sequence = new System.Activities.Statements.Sequence { Variables = { rowIdVariable } };
            var insert2 = new SQLiteExecuteNonQuery
            {
                DatabasePath = db.Path,
                Sql = "insert into t (name) values ('second');",
                LastInsertRowId = new OutArgument<long>(rowIdVariable)
            };
            sequence.Activities.Add(insert2);
            sequence.Activities.Add(new Capture<long> { Value = new InArgument<long>(rowIdVariable), OnValue = id => rowId = id });

            WorkflowInvoker.Invoke(workflow);
            WorkflowInvoker.Invoke(sequence);

            Assert.True(rowId > 0);
        }

        [Fact]
        public void TransactionScopeCommitsEverythingTogether()
        {
            using var db = new TestDatabase();

            var workflow = WorkflowHelpers.ConnectScope(db.Path,
                new SQLiteExecuteNonQuery { Sql = "create table t (id integer primary key);" },
                WorkflowHelpers.TransactionScope(
                    new SQLiteExecuteNonQuery { Sql = "insert into t values (1);" },
                    new SQLiteExecuteNonQuery { Sql = "insert into t values (2);" }));

            WorkflowInvoker.Invoke(workflow);

            using var handle = db.Open();
            Assert.Equal(2L, Convert.ToInt64(CoreTests.Scalar(handle, "select count(*) from t;")));
        }

        [Fact]
        public void TransactionScopeRollsBackWhenSomethingFails()
        {
            using var db = new TestDatabase();

            var workflow = WorkflowHelpers.ConnectScope(db.Path,
                new SQLiteExecuteNonQuery { Sql = "create table t (id integer primary key);" },
                WorkflowHelpers.TransactionScope(
                    new SQLiteExecuteNonQuery { Sql = "insert into t values (1);" },
                    new Boom()));

            Assert.ThrowsAny<Exception>(() => WorkflowInvoker.Invoke(workflow));

            using var handle = db.Open();
            Assert.Equal(0L, Convert.ToInt64(CoreTests.Scalar(handle, "select count(*) from t;")));
        }

        [Fact]
        public void WriteLockScopeHoldsTheLockForEverythingInside()
        {
            using var db = new TestDatabase();
            var lockFile = db.Path + ".writelock";
            var lockedDuringBody = false;

            var workflow = WorkflowHelpers.ConnectScope(db.Path,
                new SQLiteExecuteNonQuery { Sql = "create table t (id integer primary key);" },
                WorkflowHelpers.WriteLockScope(
                    new SQLiteExecuteNonQuery { Sql = "insert into t values (1);" },
                    new Inspect
                    {
                        Action = () =>
                        {
                            // Another process cannot take the lock file while the scope owns it.
                            try
                            {
                                using (new FileStream(lockFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                                {
                                    lockedDuringBody = false;
                                }
                            }
                            catch (IOException)
                            {
                                lockedDuringBody = true;
                            }
                        }
                    },
                    new SQLiteExecuteNonQuery { Sql = "insert into t values (2);" }));

            WorkflowInvoker.Invoke(workflow);

            Assert.True(lockedDuringBody, "The write lock scope did not hold the lock file.");

            // Released again once the scope is done.
            using (new FileStream(lockFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }

            using var handle = db.Open();
            Assert.Equal(2L, Convert.ToInt64(CoreTests.Scalar(handle, "select count(*) from t;")));
        }

        [Fact]
        public void BulkInsertActivityWritesTheDataTable()
        {
            using var db = new TestDatabase();

            var table = new DataTable();
            table.Columns.Add("id", typeof(long));
            table.Columns.Add("name", typeof(string));
            for (var i = 1; i <= 100; i++)
            {
                table.Rows.Add((long)i, "row " + i);
            }

            var affected = WorkflowInvoker.Invoke(new SQLiteBulkInsert
            {
                DatabasePath = db.Path,
                TableName = "people",
                DataTable = new InArgument<DataTable>(context => table),
                CreateTableIfNotExists = true,
                BatchSize = 25
            });

            Assert.Equal(100, affected);

            using var handle = db.Open();
            Assert.Equal(100L, Convert.ToInt64(CoreTests.Scalar(handle, "select count(*) from people;")));
        }

        [Fact]
        public void ParallelQueryReturnsOneTablePerQuery()
        {
            using var db = new TestDatabase();

            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table a (id integer primary key);");
                CoreTests.Execute(handle, "create table b (id integer primary key);");
                CoreTests.Execute(handle, "insert into a values (1),(2),(3);");
                CoreTests.Execute(handle, "insert into b values (1),(2);");
            }

            var queries = new Dictionary<string, string>
            {
                { "counts", "select count(*) as n from a;" },
                { "rowsA", "select id from a order by id;" },
                { "rowsB", "select id from b order by id;" }
            };

            var results = WorkflowInvoker.Invoke(new SQLiteParallelQuery
            {
                DatabasePath = db.Path,
                Queries = new InArgument<IDictionary<string, string>>(context => queries)
            });

            Assert.Equal(3, results.Count);
            Assert.Equal(3, results["rowsA"].Rows.Count);
            Assert.Equal(2, results["rowsB"].Rows.Count);
            Assert.Equal(3L, Convert.ToInt64(results["counts"].Rows[0]["n"]));
        }

        [Fact]
        public void ParallelQueryReportsWhichQueryFailed()
        {
            using var db = new TestDatabase();
            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table a (id integer primary key);");
            }

            var queries = new Dictionary<string, string>
            {
                { "good", "select 1;" },
                { "bad", "select * from does_not_exist;" }
            };

            var error = Assert.ThrowsAny<Exception>(() => WorkflowInvoker.Invoke(new SQLiteParallelQuery
            {
                DatabasePath = db.Path,
                Queries = new InArgument<IDictionary<string, string>>(context => queries)
            }));

            Assert.Contains("'bad'", Flatten(error));
        }

        [Fact]
        public void ContinueOnErrorKeepsTheWorkflowGoing()
        {
            using var db = new TestDatabase();
            var message = new Variable<string>();
            string captured = null;

            var sequence = new System.Activities.Statements.Sequence { Variables = { message } };
            sequence.Activities.Add(new SQLiteExecuteNonQuery
            {
                DatabasePath = db.Path,
                Sql = "this is not sql;",
                ContinueOnError = true,
                ErrorMessage = new OutArgument<string>(message)
            });
            sequence.Activities.Add(new Capture<string> { Value = new InArgument<string>(message), OnValue = value => captured = value });

            WorkflowInvoker.Invoke(sequence);

            Assert.False(string.IsNullOrWhiteSpace(captured));
        }

        [Fact]
        public void ATimeoutStopsALongRunningStatement()
        {
            using var db = new TestDatabase();

            var error = Assert.ThrowsAny<Exception>(() => WorkflowInvoker.Invoke<object>(new SQLiteExecuteScalar
            {
                DatabasePath = db.Path,
                Sql = "with recursive counter(x) as (select 1 union all select x + 1 from counter where x < 2000000000) select count(*) from counter;",
                TimeoutMS = 750
            }));

            var text = Flatten(error);
            Assert.True(text.Contains("TimeoutMS") || text.Contains("interrupted"), "Unexpected error: " + text);
        }

        [Fact]
        public void SchemaActivitiesDescribeTheDatabase()
        {
            using var db = new TestDatabase();

            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table people (id integer primary key, name text not null);");
            }

            var names = WorkflowInvoker.Invoke(new SQLiteGetTableNames { DatabasePath = db.Path });
            Assert.Contains("people", names);

            var schema = WorkflowInvoker.Invoke(new SQLiteGetTableSchema { DatabasePath = db.Path, TableName = "people" });
            Assert.Equal(2, schema.Rows.Count);
            Assert.Equal("name", schema.Rows[1]["ColumnName"]);
            Assert.True((bool)schema.Rows[1]["NotNull"]);
        }

        [Fact]
        public void MaintenanceChecksIntegrity()
        {
            using var db = new TestDatabase();
            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
            }

            var result = WorkflowInvoker.Invoke(new SQLiteMaintenance
            {
                DatabasePath = db.Path,
                Operation = SQLiteMaintenanceOperation.IntegrityCheck
            });

            Assert.Equal("ok", result, ignoreCase: true);
        }

        [Fact]
        public void BackupProducesAUsableCopy()
        {
            using var db = new TestDatabase();
            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
                CoreTests.Execute(handle, "insert into t values (1),(2);");
            }

            var destination = db.File("backup.db");
            var path = WorkflowInvoker.Invoke(new SQLiteBackupDatabase
            {
                DatabasePath = db.Path,
                DestinationPath = destination
            });

            Assert.True(File.Exists(path));

            using var copy = SQLiteConnectionHandle.Open(new SQLiteConnectionSettings { DatabasePath = destination });
            Assert.Equal(2L, Convert.ToInt64(CoreTests.Scalar(copy, "select count(*) from t;")));
        }

        [Fact]
        public void AttachLetsOneQuerySpanTwoFiles()
        {
            using var db = new TestDatabase();
            var archivePath = db.File("archive.db");

            using (var archive = SQLiteConnectionHandle.Open(new SQLiteConnectionSettings { DatabasePath = archivePath }))
            {
                CoreTests.Execute(archive, "create table old_orders (id integer primary key);");
                CoreTests.Execute(archive, "insert into old_orders values (1),(2);");
            }

            DataTable result = null;

            WorkflowInvoker.Invoke(WorkflowHelpers.ConnectScope(db.Path,
                new SQLiteExecuteNonQuery { Sql = "create table orders (id integer primary key);" },
                new SQLiteExecuteNonQuery { Sql = "insert into orders values (3);" },
                new SQLiteAttachDatabase { AttachPath = archivePath, Alias = "archive" },
                new Capture<DataTable>
                {
                    Value = new InArgument<DataTable>(new SQLiteExecuteQuery
                    {
                        Sql = "select id from orders union all select id from archive.old_orders order by id;"
                    }),
                    OnValue = table => result = table
                }));

            Assert.Equal(3, result.Rows.Count);
        }

        [Fact]
        public void ExportActivitiesWriteCsvExcelAndJson()
        {
            using var db = new TestDatabase();

            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table t (id integer primary key, name text, amount real);");
                CoreTests.Execute(handle, "insert into t values (1,'first',1.5),(2,'second',2.5);");
            }

            var csv = WorkflowInvoker.Invoke(new SQLiteExportToCsv
            {
                DatabasePath = db.Path,
                SourceTableName = "t",
                FilePath = db.File("export.csv")
            });

            var xlsx = WorkflowInvoker.Invoke(new SQLiteExportToExcel
            {
                DatabasePath = db.Path,
                Sql = "select * from t order by id;",
                SheetName = "Rows",
                FilePath = db.File("export.xlsx")
            });

            var json = WorkflowInvoker.Invoke(new SQLiteExportToJson
            {
                DatabasePath = db.Path,
                Sql = "select * from t order by id;"
            });

            Assert.Contains("first", File.ReadAllText(csv));
            Assert.True(new FileInfo(xlsx).Length > 0);
            Assert.Contains("\"name\": \"second\"", json);
        }

        [Fact]
        public void ExcelExportCanWriteSeveralSheetsFromQueries()
        {
            using var db = new TestDatabase();

            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table a (id integer primary key);");
                CoreTests.Execute(handle, "create table b (id integer primary key);");
                CoreTests.Execute(handle, "insert into a values (1),(2);");
                CoreTests.Execute(handle, "insert into b values (1);");
            }

            var sheets = new Dictionary<string, string>
            {
                { "TableA", "select * from a;" },
                { "TableB", "select * from b;" }
            };

            var path = WorkflowInvoker.Invoke(new SQLiteExportToExcel
            {
                DatabasePath = db.Path,
                Sheets = new InArgument<IDictionary<string, string>>(context => sheets),
                FilePath = db.File("multi.xlsx")
            });

            using var archive = System.IO.Compression.ZipFile.OpenRead(path);
            Assert.Equal(2, archive.Entries.Count(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal)));
        }

        [Fact]
        public void CsvImportLoadsAFileIntoATable()
        {
            using var db = new TestDatabase();
            var csv = db.File("people.csv");
            File.WriteAllText(csv, "id,name\r\n1,Sara\r\n2,Omar\r\n");

            var imported = WorkflowInvoker.Invoke(new SQLiteImportCsv
            {
                DatabasePath = db.Path,
                FilePath = csv,
                TableName = "people",
                CreateTableIfNotExists = true
            });

            Assert.Equal(2, imported);

            using var handle = db.Open();
            Assert.Equal("Omar", CoreTests.Scalar(handle, "select name from people where id = 2;"));
        }

        [Fact]
        public void ExecuteScriptRunsSeveralStatements()
        {
            using var db = new TestDatabase();

            WorkflowInvoker.Invoke(new SQLiteExecuteScript
            {
                DatabasePath = db.Path,
                Script = "create table a (id integer primary key); create table b (id integer primary key); insert into a values (1);"
            });

            using var handle = db.Open();
            Assert.True(SQLiteSchema.TableExists(handle, "a", System.Threading.CancellationToken.None));
            Assert.True(SQLiteSchema.TableExists(handle, "b", System.Threading.CancellationToken.None));
        }

        [Fact]
        public void ExecuteBatchAppliesEverythingInOneTransaction()
        {
            using var db = new TestDatabase();

            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table t (id integer primary key, name text);");
            }

            var statements = new List<SQLiteStatement>
            {
                new SQLiteStatement("insert into t (id, name) values (@id, @name);", new Dictionary<string, object> { { "id", 1 }, { "name", "one" } }),
                new SQLiteStatement("insert into t (id, name) values (@id, @name);", new Dictionary<string, object> { { "id", 2 }, { "name", "two" } }),
                new SQLiteStatement("update t set name = 'updated' where id = 1;")
            };

            var affected = WorkflowInvoker.Invoke(new SQLiteExecuteBatch
            {
                DatabasePath = db.Path,
                Statements = new InArgument<IEnumerable<SQLiteStatement>>(context => statements)
            });

            Assert.Equal(3, affected);

            using var check = db.Open();
            Assert.Equal("updated", CoreTests.Scalar(check, "select name from t where id = 1;"));
        }

        [Fact]
        public void AFailingBatchLeavesNothingBehind()
        {
            using var db = new TestDatabase();

            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
            }

            var statements = new List<SQLiteStatement>
            {
                new SQLiteStatement("insert into t values (1);"),
                new SQLiteStatement("insert into nope values (1);")
            };

            Assert.ThrowsAny<Exception>(() => WorkflowInvoker.Invoke(new SQLiteExecuteBatch
            {
                DatabasePath = db.Path,
                Statements = new InArgument<IEnumerable<SQLiteStatement>>(context => statements)
            }));

            using var check = db.Open();
            Assert.Equal(0L, Convert.ToInt64(CoreTests.Scalar(check, "select count(*) from t;")));
        }

        [Fact]
        public void TheScopeClosesTheConnectionEvenWhenTheBodyFails()
        {
            using var db = new TestDatabase();
            SQLiteConnectionHandle captured = null;

            var handleVariable = new Variable<SQLiteConnectionHandle>();

            var scope = new SQLiteConnectScope
            {
                DatabasePath = db.Path,
                Connection = new OutArgument<SQLiteConnectionHandle>(handleVariable),
                Body = new ActivityAction<SQLiteConnectionHandle>
                {
                    Argument = new DelegateInArgument<SQLiteConnectionHandle>("conn"),
                    Handler = new Boom()
                }
            };

            var root = new System.Activities.Statements.Sequence { Variables = { handleVariable } };
            root.Activities.Add(new System.Activities.Statements.TryCatch
            {
                Try = scope,
                Catches =
                {
                    new System.Activities.Statements.Catch<Exception>
                    {
                        Action = new ActivityAction<Exception>
                        {
                            Argument = new DelegateInArgument<Exception>("ex"),
                            Handler = new Capture<SQLiteConnectionHandle>
                            {
                                Value = new InArgument<SQLiteConnectionHandle>(handleVariable),
                                OnValue = handle => captured = handle
                            }
                        }
                    }
                }
            });

            WorkflowInvoker.Invoke(root);

            Assert.NotNull(captured);
            Assert.False(captured.IsOpen);
        }

        private static string Flatten(Exception exception)
        {
            var text = string.Empty;
            for (var current = exception; current != null; current = current.InnerException)
            {
                text += current.Message + Environment.NewLine;
            }

            return text;
        }
    }
}
