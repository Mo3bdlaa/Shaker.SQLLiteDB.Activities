using System;
using System.Data;
using System.Globalization;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>
    /// An open SQLite connection plus the settings, the writer lock configuration and the current
    /// transaction that belong to it. This is the object a SQLite Connect Scope hands to its children.
    /// </summary>
    public sealed class SQLiteConnectionHandle : IDisposable
    {
        private readonly object _syncRoot = new object();
        private SqliteConnection _connection;
        private int _disposed;

        private SQLiteConnectionHandle(SqliteConnection connection, SQLiteConnectionSettings settings)
        {
            _connection = connection;
            Settings = settings;
            CreatedAtUtc = DateTime.UtcNow;
        }

        /// <summary>Settings that were used to open this connection.</summary>
        public SQLiteConnectionSettings Settings { get; private set; }

        /// <summary>When the connection was opened.</summary>
        public DateTime CreatedAtUtc { get; private set; }

        /// <summary>Transaction currently running on this connection, or null.</summary>
        public SQLiteTransactionHandle CurrentTransaction { get; internal set; }

        /// <summary>
        /// Writer lock that an enclosing SQLite Write Lock Scope already holds. When this is set,
        /// individual write activities reuse it instead of taking the lock again.
        /// </summary>
        public SQLiteLockToken AmbientLock { get; internal set; }

        /// <summary>Path of the database file, or null for an in-memory database.</summary>
        public string DatabasePath
        {
            get { return Settings.ResolvedDatabasePath; }
        }

        /// <summary>True while the underlying connection is usable.</summary>
        public bool IsOpen
        {
            get
            {
                var connection = _connection;
                return _disposed == 0 && connection != null && connection.State == ConnectionState.Open;
            }
        }

        /// <summary>The underlying ADO.NET connection. Prefer the <c>Execute</c> methods so that locking and retries apply.</summary>
        public SqliteConnection Connection
        {
            get
            {
                ThrowIfDisposed();
                return _connection;
            }
        }

        /// <summary>Object used to serialize access to the connection inside this process.</summary>
        internal object SyncRoot
        {
            get { return _syncRoot; }
        }

        /// <summary>Opens a connection for the given settings.</summary>
        public static SQLiteConnectionHandle Open(SQLiteConnectionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            var effective = settings.Clone();
            SqliteConnection connection;

            try
            {
                connection = effective.Open();
            }
            catch (SqliteException ex)
            {
                var target = effective.ResolvedDatabasePath ?? effective.DatabasePath ?? "(connection string)";

                // 26 = "file is not a database": either the file is encrypted and the password is wrong
                // or missing, or it is not a SQLite file at all.
                if ((ex.SqliteErrorCode & 0xFF) == 26)
                {
                    throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                        string.IsNullOrEmpty(effective.Password)
                            ? "The database '{0}' could not be read. It is encrypted, so its password has to be supplied in 'Password' (or it is not a SQLite database at all)."
                            : "The database '{0}' could not be read with the password that was supplied. Check 'Password'.",
                        target), ex);
                }

                throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                    "Could not open the SQLite database '{0}': {1}", target, ex.Message), ex);
            }

            return new SQLiteConnectionHandle(connection, effective);
        }

        /// <summary>
        /// Runs <paramref name="work"/> against the connection. Access is serialized inside the process,
        /// the current transaction is passed along and transient lock errors are retried.
        /// </summary>
        public T Execute<T>(Func<SqliteConnection, SqliteTransaction, T> work, CancellationToken cancellationToken)
        {
            return Execute(work, cancellationToken, null);
        }

        /// <summary>
        /// Same as <c>Execute</c> but with a
        /// retry policy that overrides the one of the connection. Operations that are not safe to replay
        /// pass a policy with a single attempt.
        /// </summary>
        public T Execute<T>(Func<SqliteConnection, SqliteTransaction, T> work, CancellationToken cancellationToken, SQLiteRetryOptions retryOverride)
        {
            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            ThrowIfDisposed();

            CancellationTokenRegistration registration = default(CancellationTokenRegistration);
            var connection = _connection;

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() => SQLiteNative.Interrupt(connection));
            }

            try
            {
                lock (_syncRoot)
                {
                    return SQLiteRetryPolicy.Execute(
                        () => work(_connection, CurrentTransaction == null ? null : CurrentTransaction.Transaction),
                        retryOverride ?? Settings.Retry,
                        cancellationToken);
                }
            }
            finally
            {
                registration.Dispose();
            }
        }

        /// <summary>
        /// Takes the writer lock for this database, unless an enclosing scope already holds it or the
        /// connection is read only.
        /// </summary>
        public SQLiteLockToken AcquireWriteLock(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (AmbientLock != null && AmbientLock.IsHeld)
            {
                return SQLiteLockToken.Nested(AmbientLock);
            }

            if (Settings.IsReadOnly || Settings.IsInMemory)
            {
                return SQLiteLockToken.Disabled();
            }

            var lockOptions = Settings.Lock ?? new SQLiteLockOptions();
            return SQLiteWriteLock.Acquire(
                Settings.DatabaseKey,
                lockOptions.ResolveLockFilePath(DatabasePath),
                lockOptions,
                cancellationToken);
        }

        /// <summary>Takes the reader lock, which only does something when <c>LockReads</c> is switched on.</summary>
        public SQLiteLockToken AcquireReadLock(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var lockOptions = Settings.Lock ?? new SQLiteLockOptions();
            if (!lockOptions.LockReads)
            {
                return SQLiteLockToken.Disabled();
            }

            return AcquireWriteLock(cancellationToken);
        }

        /// <summary>
        /// Starts a transaction. When one is already running, a SAVEPOINT is used so that scopes can nest.
        /// </summary>
        public SQLiteTransactionHandle BeginTransaction(SQLiteTransactionMode mode)
        {
            ThrowIfDisposed();

            lock (_syncRoot)
            {
                if (CurrentTransaction != null && CurrentTransaction.IsActive)
                {
                    return SQLiteTransactionHandle.CreateSavepoint(this, CurrentTransaction);
                }

                var deferred = mode == SQLiteTransactionMode.Deferred;
                SqliteTransaction transaction;

                try
                {
                    transaction = _connection.BeginTransaction(IsolationLevel.Serializable, deferred);
                }
                catch (SqliteException ex)
                {
                    throw new SQLiteActivityException(
                        "Could not start a SQLite transaction: " + ex.Message, ex);
                }

                var handle = SQLiteTransactionHandle.CreateRoot(this, transaction, mode);
                CurrentTransaction = handle;
                return handle;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed != 0)
            {
                throw new ObjectDisposedException("SQLiteConnectionHandle",
                    "This SQLite connection was already closed. Keep write activities inside the SQLite Connect Scope that opened the connection.");
            }
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            {
                return;
            }

            var transaction = CurrentTransaction;
            if (transaction != null && transaction.IsActive)
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // Closing the connection discards it anyway.
                }
            }

            var connection = Interlocked.Exchange(ref _connection, null);
            if (connection != null)
            {
                try
                {
                    connection.Close();
                }
                finally
                {
                    connection.Dispose();
                }
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "SQLite connection to '{0}' ({1}, {2})",
                DatabasePath ?? ":memory:",
                Settings.OpenMode,
                IsOpen ? "open" : "closed");
        }
    }
}
