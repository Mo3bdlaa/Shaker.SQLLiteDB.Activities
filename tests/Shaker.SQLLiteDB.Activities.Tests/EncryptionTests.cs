using System;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Data.Sqlite;
using Shaker.SQLLiteDB.Activities.Activities;
using Shaker.SQLLiteDB.Activities.Activities.Connection;
using Shaker.SQLLiteDB.Activities.Activities.Query;
using Shaker.SQLLiteDB.Activities.Activities.Write;
using Shaker.SQLLiteDB.Activities.Activities.Export;
using Shaker.SQLLiteDB.Activities.Activities.Schema;
using Shaker.SQLLiteDB.Activities.Activities.Maintenance;
using Shaker.SQLLiteDB.Activities.Core;
using System.Activities;
using Xunit;

namespace Shaker.SQLLiteDB.Activities.Tests
{
    public class EncryptionTests
    {
        private static readonly CancellationToken None = CancellationToken.None;

        [Fact]
        public void TheBundledEngineCanDoEncryption()
        {
            Assert.True(SQLiteNative.IsEncryptionSupported);
            Assert.Contains("4.", SQLiteNative.CipherVersion);
        }

        [Fact]
        public void ADatabaseCreatedWithAPasswordIsUnreadableWithoutIt()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Password = "s3cret";

            using (var handle = SQLiteConnectionHandle.Open(settings))
            {
                CoreTests.Execute(handle, "create table t (id integer primary key, v text);");
                CoreTests.Execute(handle, "insert into t values (1, 'confidential');");
            }

            // The file does not even start with the usual SQLite header any more.
            var header = Encoding.ASCII.GetString(File.ReadAllBytes(db.Path), 0, 15);
            Assert.NotEqual("SQLite format 3", header);

            using (var withPassword = SQLiteConnectionHandle.Open(settings))
            {
                Assert.Equal("confidential", CoreTests.Scalar(withPassword, "select v from t;"));
            }

            Assert.Throws<SQLiteActivityException>(() => SQLiteConnectionHandle.Open(db.Settings()));

            var wrong = db.Settings();
            wrong.Password = "wrong";
            Assert.Throws<SQLiteActivityException>(() => SQLiteConnectionHandle.Open(wrong));
        }

        [Fact]
        public void EncryptionWorksTogetherWithWalAndTheWriterLock()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Password = "s3cret";

            using var handle = SQLiteConnectionHandle.Open(settings);

            var journal = handle.Execute((connection, transaction) =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = "pragma journal_mode;";
                return Convert.ToString(command.ExecuteScalar());
            }, None);

            Assert.Equal("wal", journal, ignoreCase: true);

            using (handle.AcquireWriteLock(None))
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
                CoreTests.Execute(handle, "insert into t values (1);");
            }

            Assert.Equal(1L, Convert.ToInt64(CoreTests.Scalar(handle, "select count(*) from t;")));
        }

        [Fact]
        public void IsEncryptedTellsThemApart()
        {
            using var db = new TestDatabase();

            using (var plain = db.Open())
            {
                CoreTests.Execute(plain, "create table t (id integer primary key);");
            }

            Assert.False(SQLiteEncryption.IsEncrypted(db.Path));

            var encryptedPath = db.File("secret.db");
            var settings = new SQLiteConnectionSettings { DatabasePath = encryptedPath, Password = "pw" };

            using (var encrypted = SQLiteConnectionHandle.Open(settings))
            {
                CoreTests.Execute(encrypted, "create table t (id integer primary key);");
            }

            Assert.True(SQLiteEncryption.IsEncrypted(encryptedPath));
            Assert.False(SQLiteEncryption.IsEncrypted(db.File("does-not-exist.db")));
        }

        [Fact]
        public void APlainDatabaseCanBeEncryptedKeepingItsContent()
        {
            using var db = new TestDatabase();

            using (var plain = db.Open())
            {
                CoreTests.Execute(plain, "create table people (id integer primary key, name text);");
                CoreTests.Execute(plain, "create index ix_people_name on people (name);");
                CoreTests.Execute(plain, "create view v_people as select name from people;");
                CoreTests.Execute(plain, "insert into people values (1, 'Sara'), (2, 'Omar');");
            }

            var result = SQLiteEncryption.SetPassword(db.Settings(), "topsecret", true, None);

            Assert.Equal(SQLitePasswordOperation.Encrypted, result.Operation);
            Assert.True(File.Exists(result.BackupPath));
            Assert.True(SQLiteEncryption.IsEncrypted(db.Path));

            var settings = db.Settings();
            settings.Password = "topsecret";

            using var encrypted = SQLiteConnectionHandle.Open(settings);
            Assert.Equal(2L, Convert.ToInt64(CoreTests.Scalar(encrypted, "select count(*) from people;")));
            Assert.Equal("Omar", CoreTests.Scalar(encrypted, "select name from people where id = 2;"));

            // Indexes and views survive the conversion.
            var objects = SQLiteCommandExecutor.ExecuteQuery(encrypted,
                "select name from sqlite_master where name in ('ix_people_name','v_people');", null, null,
                SQLiteColumnTyping.AllText, 0, null, None);
            Assert.Equal(2, objects.Rows.Count);
        }

        [Fact]
        public void ThePasswordOfAnEncryptedDatabaseCanBeChangedInPlace()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Password = "first";

            using (var handle = SQLiteConnectionHandle.Open(settings))
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
                CoreTests.Execute(handle, "insert into t values (1);");
            }

            var result = SQLiteEncryption.SetPassword(settings, "second", true, None);

            Assert.Equal(SQLitePasswordOperation.PasswordChanged, result.Operation);
            Assert.Null(result.BackupPath);

            var updated = db.Settings();
            updated.Password = "second";
            using (var handle = SQLiteConnectionHandle.Open(updated))
            {
                Assert.Equal(1L, Convert.ToInt64(CoreTests.Scalar(handle, "select count(*) from t;")));
            }

            var old = db.Settings();
            old.Password = "first";
            Assert.Throws<SQLiteActivityException>(() => SQLiteConnectionHandle.Open(old));
        }

        [Fact]
        public void EncryptionCanBeRemovedAgain()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Password = "s3cret";

            using (var handle = SQLiteConnectionHandle.Open(settings))
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
                CoreTests.Execute(handle, "insert into t values (1),(2),(3);");
            }

            var result = SQLiteEncryption.SetPassword(settings, string.Empty, false, None);

            Assert.Equal(SQLitePasswordOperation.Decrypted, result.Operation);
            Assert.Null(result.BackupPath);
            Assert.False(SQLiteEncryption.IsEncrypted(db.Path));

            using var plain = db.Open();
            Assert.Equal(3L, Convert.ToInt64(CoreTests.Scalar(plain, "select count(*) from t;")));
        }

        [Fact]
        public void AFailedConversionLeavesTheOriginalAlone()
        {
            using var db = new TestDatabase();

            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
                CoreTests.Execute(handle, "insert into t values (1);");
            }

            // Claiming a password the database does not have makes the conversion fail.
            var wrong = db.Settings();
            wrong.Password = "not the password";

            Assert.ThrowsAny<Exception>(() => SQLiteEncryption.SetPassword(wrong, "new", true, None));

            Assert.False(SQLiteEncryption.IsEncrypted(db.Path));
            Assert.False(File.Exists(db.Path + ".converting.tmp"));

            using var handle2 = db.Open();
            Assert.Equal(1L, Convert.ToInt64(CoreTests.Scalar(handle2, "select count(*) from t;")));
        }

        [Fact]
        public void TheActivityEncryptsAndReportsWhatItDid()
        {
            using var db = new TestDatabase();

            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
                CoreTests.Execute(handle, "insert into t values (1);");
            }

            var message = WorkflowInvoker.Invoke(new SQLiteSetPassword
            {
                DatabasePath = db.Path,
                NewPassword = "workflow-secret"
            });

            Assert.Contains("Encrypted", message);
            Assert.True(SQLiteEncryption.IsEncrypted(db.Path));

            // And the ordinary activities can work with it once the password is supplied.
            var rows = WorkflowInvoker.Invoke<object>(new SQLiteExecuteScalar
            {
                DatabasePath = db.Path,
                Password = "workflow-secret",
                Sql = "select count(*) from t;"
            });

            Assert.Equal(1L, Convert.ToInt64(rows));
        }

        [Fact]
        public void TheActivityRefusesToRunInsideAConnectScope()
        {
            using var db = new TestDatabase();

            var workflow = WorkflowHelpers.ConnectScope(db.Path,
                new SQLiteExecuteNonQuery { Sql = "create table t (id integer primary key);" },
                new SQLiteSetPassword { NewPassword = "nope" });

            var error = Assert.ThrowsAny<Exception>(() => WorkflowInvoker.Invoke(workflow));
            var text = string.Empty;
            for (var current = error; current != null; current = current.InnerException)
            {
                text += current.Message;
            }

            Assert.Contains("cannot run inside a SQLite Connect Scope", text);
        }

        [Fact]
        public void TheActivityAsksForTheCurrentPasswordWhenTheDatabaseIsEncrypted()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Password = "existing";

            using (var handle = SQLiteConnectionHandle.Open(settings))
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
            }

            var error = Assert.ThrowsAny<Exception>(() => WorkflowInvoker.Invoke(new SQLiteSetPassword
            {
                DatabasePath = db.Path,
                NewPassword = "another"
            }));

            var text = string.Empty;
            for (var current = error; current != null; current = current.InnerException)
            {
                text += current.Message;
            }

            Assert.Contains("current password", text);
        }


        [Fact]
        public void ABackupCanBeGivenItsOwnPassword()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Password = "source-pw";

            using (var handle = SQLiteConnectionHandle.Open(settings))
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
                CoreTests.Execute(handle, "insert into t values (1),(2);");
            }

            var copy = WorkflowInvoker.Invoke(new SQLiteBackupDatabase
            {
                DatabasePath = db.Path,
                Password = "source-pw",
                DestinationPath = db.File("copy.db"),
                BackupPassword = "archive-pw"
            });

            var archiveSettings = new SQLiteConnectionSettings { DatabasePath = copy, Password = "archive-pw" };
            using (var archive = SQLiteConnectionHandle.Open(archiveSettings))
            {
                Assert.Equal(2L, Convert.ToInt64(CoreTests.Scalar(archive, "select count(*) from t;")));
            }

            var withSourcePassword = new SQLiteConnectionSettings { DatabasePath = copy, Password = "source-pw" };
            Assert.Throws<SQLiteActivityException>(() => SQLiteConnectionHandle.Open(withSourcePassword));
        }

        [Fact]
        public void APlainDatabaseIsStillBackedUpWithTheOnlineBackupApi()
        {
            using var db = new TestDatabase();

            using (var handle = db.Open())
            {
                CoreTests.Execute(handle, "create table t (id integer primary key);");
                CoreTests.Execute(handle, "insert into t values (1);");
            }

            var copy = WorkflowInvoker.Invoke(new SQLiteBackupDatabase
            {
                DatabasePath = db.Path,
                DestinationPath = db.File("plain-copy.db")
            });

            Assert.False(SQLiteEncryption.IsEncrypted(copy));
        }

        [Fact]
        public void ExportAndBackupStillWorkOnAnEncryptedDatabase()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Password = "s3cret";

            using (var handle = SQLiteConnectionHandle.Open(settings))
            {
                CoreTests.Execute(handle, "create table t (id integer primary key, name text);");
                CoreTests.Execute(handle, "insert into t values (1,'first'),(2,'second');");
            }

            var csv = WorkflowInvoker.Invoke(new SQLiteExportToCsv
            {
                DatabasePath = db.Path,
                Password = "s3cret",
                SourceTableName = "t",
                FilePath = db.File("export.csv")
            });

            Assert.Contains("second", File.ReadAllText(csv));

            var backup = WorkflowInvoker.Invoke(new SQLiteBackupDatabase
            {
                DatabasePath = db.Path,
                Password = "s3cret",
                DestinationPath = db.File("copy.db")
            });

            // The online backup of an encrypted database keeps the encryption of the source.
            Assert.True(SQLiteEncryption.IsEncrypted(backup));

            var copySettings = new SQLiteConnectionSettings { DatabasePath = backup, Password = "s3cret" };
            using var copy = SQLiteConnectionHandle.Open(copySettings);
            Assert.Equal(2L, Convert.ToInt64(CoreTests.Scalar(copy, "select count(*) from t;")));
        }
    }
}
