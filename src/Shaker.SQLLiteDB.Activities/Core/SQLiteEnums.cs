using System;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>How the database file is opened.</summary>
    public enum SQLiteOpenMode
    {
        /// <summary>Open for reading and writing, create the file when it does not exist (default).</summary>
        ReadWriteCreate = 0,
        /// <summary>Open for reading and writing, fail when the file does not exist.</summary>
        ReadWrite = 1,
        /// <summary>Open read only. Readers never block each other and never take the write lock.</summary>
        ReadOnly = 2,
        /// <summary>Pure in-memory database. Useful for staging and unit tests.</summary>
        Memory = 3
    }

    /// <summary>SQLite journal mode (PRAGMA journal_mode).</summary>
    public enum SQLiteJournalMode
    {
        /// <summary>Write Ahead Logging: readers do not block the writer and the writer does not block readers. Recommended and the default.</summary>
        Wal = 0,
        /// <summary>Leave whatever the database already uses.</summary>
        Unchanged = 1,
        Delete = 2,
        Truncate = 3,
        Persist = 4,
        Memory = 5,
        Off = 6
    }

    /// <summary>SQLite durability level (PRAGMA synchronous).</summary>
    public enum SQLiteSynchronousMode
    {
        /// <summary>Leave the engine default.</summary>
        Unchanged = 0,
        Off = 1,
        /// <summary>Safe and fast in WAL mode. Recommended.</summary>
        Normal = 2,
        Full = 3,
        Extra = 4
    }

    /// <summary>SQLite temporary storage location (PRAGMA temp_store).</summary>
    public enum SQLiteTempStore
    {
        Unchanged = 0,
        File = 1,
        Memory = 2
    }

    /// <summary>Scope of the writer lock that serializes write operations.</summary>
    public enum SQLiteLockScope
    {
        /// <summary>Serialize writers across every process and machine that can reach the lock file (default). Works on local disks and on file shares.</summary>
        Machine = 0,
        /// <summary>Serialize writers inside the current process only (for example several parallel workflow branches in one robot).</summary>
        Process = 1,
        /// <summary>No extra lock. Rely only on SQLite's own busy handler.</summary>
        None = 2
    }

    /// <summary>Conflict handling for insert style write operations.</summary>
    public enum SQLiteConflictPolicy
    {
        /// <summary>Fail the statement on a constraint violation (SQLite default).</summary>
        Abort = 0,
        /// <summary>Silently skip rows that violate a constraint (INSERT OR IGNORE).</summary>
        Ignore = 1,
        /// <summary>Delete the conflicting row and insert the new one (INSERT OR REPLACE).</summary>
        Replace = 2,
        /// <summary>Roll back the whole transaction on a constraint violation.</summary>
        Rollback = 3,
        /// <summary>Update the existing row (UPSERT). Requires <c>KeyColumns</c> to be set.</summary>
        Upsert = 4
    }

    /// <summary>How result columns are typed when a query is converted to a DataTable.</summary>
    public enum SQLiteColumnTyping
    {
        /// <summary>Infer the column type from the values that were actually returned. Mixed columns become <see cref="object"/>.</summary>
        Auto = 0,
        /// <summary>Use the type reported by the data reader for every column.</summary>
        Declared = 1,
        /// <summary>Return every column as <see cref="string"/>. Handy when the data is written straight to a report.</summary>
        AllText = 2
    }

    /// <summary>Maintenance command executed by the <c>SQLiteMaintenance</c> activity.</summary>
    public enum SQLiteMaintenanceOperation
    {
        /// <summary>Rebuild the database file and reclaim free pages (VACUUM).</summary>
        Vacuum = 0,
        /// <summary>Refresh the query planner statistics (ANALYZE).</summary>
        Analyze = 1,
        /// <summary>Run the lightweight PRAGMA optimize.</summary>
        Optimize = 2,
        /// <summary>Fold the write ahead log back into the database file (PRAGMA wal_checkpoint(TRUNCATE)).</summary>
        WalCheckpoint = 3,
        /// <summary>Verify the structural integrity of the database (PRAGMA integrity_check).</summary>
        IntegrityCheck = 4,
        /// <summary>Verify foreign key constraints (PRAGMA foreign_key_check).</summary>
        ForeignKeyCheck = 5,
        /// <summary>Rebuild indexes (REINDEX).</summary>
        Reindex = 6
    }

    /// <summary>Transaction isolation requested by the transaction scope.</summary>
    public enum SQLiteTransactionMode
    {
        /// <summary>Take the write lock on the first write (BEGIN DEFERRED). Default.</summary>
        Deferred = 0,
        /// <summary>Take the reserved write lock right away (BEGIN IMMEDIATE). Recommended for write scopes under contention.</summary>
        Immediate = 1,
        /// <summary>Take an exclusive lock right away (BEGIN EXCLUSIVE).</summary>
        Exclusive = 2
    }
}
