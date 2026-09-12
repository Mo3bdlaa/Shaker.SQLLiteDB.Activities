using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shaker.SQLLiteDB.Activities.Core;
using Xunit;

namespace Shaker.SQLLiteDB.Activities.Tests
{
    public class LockingTests
    {
        private static readonly CancellationToken None = CancellationToken.None;

        [Fact]
        public void WritersAreSerializedAndNeverOverlap()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();
            CoreTests.Execute(handle, "create table t (id integer);");

            var concurrent = 0;
            var maxConcurrent = 0;
            var gate = new object();

            Parallel.For(0, 8, _ =>
            {
                using (handle.AcquireWriteLock(None))
                {
                    lock (gate)
                    {
                        concurrent++;
                        maxConcurrent = Math.Max(maxConcurrent, concurrent);
                    }

                    Thread.Sleep(20);

                    lock (gate)
                    {
                        concurrent--;
                    }
                }
            });

            Assert.Equal(1, maxConcurrent);
        }

        [Fact]
        public void LockFileIsCreatedNextToTheDatabase()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            using (var token = handle.AcquireWriteLock(None))
            {
                Assert.True(token.IsHeld);
                Assert.True(File.Exists(db.Path + ".writelock"));
                Assert.Contains(Environment.MachineName, SQLiteWriteLock.ReadOwnerInfo(db.Path + ".writelock"));
            }
        }

        [Fact]
        public void ALockHeldByAnotherProcessMakesTheWriterTimeOut()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Lock.AcquireTimeoutMilliseconds = 400;

            using var handle = SQLiteConnectionHandle.Open(settings);
            var lockFile = db.Path + ".writelock";

            // Holding the file exclusively is exactly what another robot's process would do.
            using (new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var watch = Stopwatch.StartNew();
                var error = Assert.Throws<SQLiteLockTimeoutException>(() => handle.AcquireWriteLock(None));
                watch.Stop();

                Assert.Contains("writer lock", error.Message);
                Assert.True(watch.ElapsedMilliseconds >= 350, "It gave up before the configured timeout.");
            }

            // Once the other holder is gone the lock is available again.
            using var token = handle.AcquireWriteLock(None);
            Assert.True(token.IsHeld);
        }

        [Fact]
        public void ProcessScopeDoesNotTouchTheFileSystem()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Lock.Scope = SQLiteLockScope.Process;

            using var handle = SQLiteConnectionHandle.Open(settings);
            using (handle.AcquireWriteLock(None))
            {
                Assert.False(File.Exists(db.Path + ".writelock"));
            }
        }

        [Fact]
        public void LockScopeNoneHandsOutADisabledToken()
        {
            using var db = new TestDatabase();
            var settings = db.Settings();
            settings.Lock.Scope = SQLiteLockScope.None;

            using var handle = SQLiteConnectionHandle.Open(settings);
            using var token = handle.AcquireWriteLock(None);
            Assert.False(token.IsHeld);
        }

        [Fact]
        public void AnAmbientLockIsReusedInsteadOfTakenAgain()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            using var outer = handle.AcquireWriteLock(None);
            handle.AmbientLock = outer;

            using var inner = handle.AcquireWriteLock(None);
            Assert.True(inner.IsNested);
            Assert.True(inner.IsHeld);
        }

        [Fact]
        public void ReadsDoNotTakeTheLockUnlessAskedTo()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            using (var token = handle.AcquireReadLock(None))
            {
                Assert.False(token.IsHeld);
            }

            var settings = db.Settings();
            settings.Lock.LockReads = true;

            using var locking = SQLiteConnectionHandle.Open(settings);
            using (var token = locking.AcquireReadLock(None))
            {
                Assert.True(token.IsHeld);
            }
        }

        [Fact]
        public void ReadersKeepWorkingWhileAWriterHoldsTheLock()
        {
            using var db = new TestDatabase();
            using var writer = db.Open();
            CoreTests.Execute(writer, "create table t (id integer);");
            CoreTests.Execute(writer, "insert into t values (1);");

            using var writeLock = writer.AcquireWriteLock(None);

            // WAL: a reader on another connection sees the committed data without waiting for the writer.
            var readerSettings = db.Settings();
            readerSettings.OpenMode = SQLiteOpenMode.ReadOnly;

            using var reader = SQLiteConnectionHandle.Open(readerSettings);
            Assert.Equal(1L, CoreTests.Scalar(reader, "select count(*) from t;"));
        }

        [Fact]
        public void ManyProcessesWritingThroughTheLockLoseNoRows()
        {
            using var db = new TestDatabase();

            using (var setup = db.Open())
            {
                CoreTests.Execute(setup, "create table t (id integer primary key autoincrement, writer text);");
            }

            const int writers = 6;
            const int rowsPerWriter = 25;
            var errors = new List<string>();

            Parallel.For(0, writers, index =>
            {
                try
                {
                    // Each task opens its own connection, which is as close to a separate robot as a
                    // unit test gets.
                    using var handle = db.Open();

                    for (var row = 0; row < rowsPerWriter; row++)
                    {
                        using (handle.AcquireWriteLock(None))
                        {
                            CoreTests.Execute(handle, "insert into t (writer) values ('w" + index + "');");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (errors)
                    {
                        errors.Add(ex.Message);
                    }
                }
            });

            Assert.Empty(errors);

            using var check = db.Open();
            Assert.Equal((long)(writers * rowsPerWriter), Convert.ToInt64(CoreTests.Scalar(check, "select count(*) from t;")));
        }


        [Fact]
        public void ALockReleasedOnAnotherThreadIsReallyReleased()
        {
            // A scope takes the lock on one workflow thread and can release it on another. If the
            // release left an "already held" marker behind on the first thread, the next write on that
            // thread would silently run without the lock.
            using var db = new TestDatabase();
            using var handle = db.Open();

            SQLiteLockToken first = null;
            var acquire = new Thread(() => first = handle.AcquireWriteLock(None));
            acquire.Start();
            acquire.Join();

            var release = new Thread(() => first.Dispose());
            release.Start();
            release.Join();

            SQLiteLockToken second = null;
            var again = new Thread(() => second = handle.AcquireWriteLock(None));
            again.Start();
            again.Join();

            try
            {
                Assert.True(second.IsHeld);
                Assert.False(second.IsNested);

                // And it really holds the file: nobody else can take it.
                Assert.Throws<IOException>(() =>
                    new FileStream(db.Path + ".writelock", FileMode.Open, FileAccess.ReadWrite, FileShare.None));
            }
            finally
            {
                second.Dispose();
            }
        }

        [Fact]
        public void AReleasedLockLetsTheNextWriterIn()
        {
            using var db = new TestDatabase();
            using var handle = db.Open();

            using (handle.AcquireWriteLock(None))
            {
            }

            var taken = false;
            var thread = new Thread(() =>
            {
                using (var token = handle.AcquireWriteLock(None))
                {
                    taken = token.IsHeld;
                }
            });

            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "The second writer never got the lock.");
            Assert.True(taken);
        }

        [Fact]
        public void RetryPolicyRecognizesBusyErrors()
        {
            var attempts = 0;

            var result = SQLiteRetryPolicy.Execute(() =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new Microsoft.Data.Sqlite.SqliteException("database is locked", 5);
                }

                return attempts;
            }, new SQLiteRetryOptions { MaxAttempts = 5, InitialDelayMilliseconds = 1 }, None);

            Assert.Equal(3, result);
        }

        [Fact]
        public void RetryPolicyGivesUpWithAHelpfulMessage()
        {
            var error = Assert.Throws<SQLiteActivityException>(() => SQLiteRetryPolicy.Execute<int>(
                () => throw new Microsoft.Data.Sqlite.SqliteException("database is locked", 5),
                new SQLiteRetryOptions { MaxAttempts = 2, InitialDelayMilliseconds = 1 }, None));

            Assert.Contains("after 2 attempts", error.Message);
        }

        [Fact]
        public void RetryPolicyDoesNotSwallowRealErrors()
        {
            var attempts = 0;

            Assert.Throws<InvalidOperationException>(() => SQLiteRetryPolicy.Execute<int>(() =>
            {
                attempts++;
                throw new InvalidOperationException("syntax error");
            }, new SQLiteRetryOptions { MaxAttempts = 5, InitialDelayMilliseconds = 1 }, None));

            Assert.Equal(1, attempts);
        }
    }
}
