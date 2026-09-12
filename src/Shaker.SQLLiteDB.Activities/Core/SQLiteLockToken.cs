using System;
using System.IO;
using System.Threading;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>
    /// Handle on a taken writer lock. Dispose it to let the next writer in.
    /// A token can be published into the workflow so that a whole group of activities shares one lock.
    /// </summary>
    public sealed class SQLiteLockToken : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly FileStream _fileLock;
        private readonly SQLiteLockOptions _options;
        private readonly SQLiteLockToken _owner;
        private int _disposed;

        internal SQLiteLockToken(string databaseKey, string lockFilePath, SemaphoreSlim semaphore, FileStream fileLock, SQLiteLockOptions options, long waitedMilliseconds)
        {
            DatabaseKey = databaseKey;
            LockFilePath = lockFilePath;
            AcquiredAtUtc = DateTime.UtcNow;
            WaitedMilliseconds = waitedMilliseconds;
            IsHeld = true;
            _semaphore = semaphore;
            _fileLock = fileLock;
            _options = options;
        }

        private SQLiteLockToken(SQLiteLockToken owner, bool held)
        {
            _owner = owner;
            IsHeld = held;
            IsNested = owner != null;
            DatabaseKey = owner != null ? owner.DatabaseKey : null;
            LockFilePath = owner != null ? owner.LockFilePath : null;
            AcquiredAtUtc = owner != null ? owner.AcquiredAtUtc : DateTime.UtcNow;
        }

        /// <summary>Database this lock protects.</summary>
        public string DatabaseKey { get; private set; }

        /// <summary>Lock file backing this token, when the scope is <see cref="SQLiteLockScope.Machine"/>.</summary>
        public string LockFilePath { get; private set; }

        /// <summary>When the lock was taken.</summary>
        public DateTime AcquiredAtUtc { get; private set; }

        /// <summary>How long the caller had to wait for the lock.</summary>
        public long WaitedMilliseconds { get; private set; }

        /// <summary>False for a token created while locking is switched off.</summary>
        public bool IsHeld { get; private set; }

        /// <summary>True when this token only borrows a lock that an outer scope already holds.</summary>
        public bool IsNested { get; private set; }

        /// <summary>How long the lock has been held so far.</summary>
        public TimeSpan HeldFor
        {
            get { return DateTime.UtcNow - AcquiredAtUtc; }
        }

        internal static SQLiteLockToken Disabled()
        {
            return new SQLiteLockToken(null, false);
        }

        internal static SQLiteLockToken Nested(SQLiteLockToken owner)
        {
            return new SQLiteLockToken(owner, true);
        }

        /// <summary>Releases the lock. Nested and disabled tokens do nothing.</summary>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            {
                return;
            }

            if (_owner != null || !IsHeld)
            {
                // Nested or disabled: the outer scope owns the real lock.
                return;
            }

            try
            {
                if (_fileLock != null)
                {
                    _fileLock.Dispose();
                    TryRemoveOwnerFile();
                }
            }
            finally
            {
                IsHeld = false;
                if (_semaphore != null)
                {
                    _semaphore.Release();
                }
            }
        }

        private void TryRemoveOwnerFile()
        {
            if (_options == null || !_options.WriteOwnerInfo || string.IsNullOrEmpty(LockFilePath))
            {
                return;
            }

            try
            {
                var path = LockFilePath + ".owner";
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // The next holder overwrites it anyway.
            }
        }

        public override string ToString()
        {
            if (!IsHeld)
            {
                return "SQLite writer lock: disabled";
            }

            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "SQLite writer lock on '{0}'{1}, held for {2:N0} ms",
                DatabaseKey, IsNested ? " (nested)" : string.Empty, HeldFor.TotalMilliseconds);
        }
    }
}
