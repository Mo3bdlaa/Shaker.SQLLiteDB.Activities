using System;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>
    /// Configuration of the writer lock. SQLite allows many readers at the same time but only one writer;
    /// this lock turns "one writer wins, the others get SQLITE_BUSY" into "writers queue up in an orderly way",
    /// across processes and across machines when the database sits on a file share.
    /// </summary>
    public class SQLiteLockOptions
    {
        /// <summary>How far the lock reaches. Default is <see cref="SQLiteLockScope.Machine"/> (a lock file next to the database).</summary>
        public SQLiteLockScope Scope { get; set; } = SQLiteLockScope.Machine;

        /// <summary>
        /// Lock file to use. When empty, <c>&lt;database&gt;.writelock</c> next to the database file is used.
        /// Point several robots at the same path (for example a UNC path) to serialize them.
        /// </summary>
        public string LockFilePath { get; set; }

        /// <summary>How long a writer waits for the lock before the activity fails. Default 60 seconds.</summary>
        public int AcquireTimeoutMilliseconds { get; set; } = 60000;

        /// <summary>Delay between two attempts to take the lock file. Default 50 ms, with a small random jitter.</summary>
        public int PollIntervalMilliseconds { get; set; } = 50;

        /// <summary>
        /// Write a side car file with machine name, process id and user of the current holder.
        /// It makes lock timeouts diagnosable; turn it off on read only folders.
        /// </summary>
        public bool WriteOwnerInfo { get; set; } = true;

        /// <summary>Also take the lock for read operations. Only useful for non-WAL databases on a file share.</summary>
        public bool LockReads { get; set; }

        /// <summary>Resolves the lock file to use for a given database file.</summary>
        public string ResolveLockFilePath(string databasePath)
        {
            if (!string.IsNullOrWhiteSpace(LockFilePath))
            {
                return LockFilePath;
            }

            return string.IsNullOrEmpty(databasePath) ? null : databasePath + ".writelock";
        }

        public SQLiteLockOptions Clone()
        {
            return new SQLiteLockOptions
            {
                Scope = Scope,
                LockFilePath = LockFilePath,
                AcquireTimeoutMilliseconds = AcquireTimeoutMilliseconds,
                PollIntervalMilliseconds = PollIntervalMilliseconds,
                WriteOwnerInfo = WriteOwnerInfo,
                LockReads = LockReads
            };
        }
    }
}
