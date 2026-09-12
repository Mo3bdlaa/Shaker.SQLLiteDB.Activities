using System;
using System.Globalization;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>
    /// A running transaction. The outermost scope owns a real SQLite transaction; nested scopes get a
    /// SAVEPOINT, so a failing inner scope can be rolled back without losing the outer work.
    /// </summary>
    public sealed class SQLiteTransactionHandle : IDisposable
    {
        private static int _savepointCounter;

        private readonly SQLiteConnectionHandle _handle;
        private readonly SQLiteTransactionHandle _parent;
        private int _completed;

        private SQLiteTransactionHandle(SQLiteConnectionHandle handle, SqliteTransaction transaction, SQLiteTransactionHandle parent, string savepointName, SQLiteTransactionMode mode)
        {
            _handle = handle;
            _parent = parent;
            Transaction = transaction;
            SavepointName = savepointName;
            Mode = mode;
            StartedAtUtc = DateTime.UtcNow;
        }

        /// <summary>The ADO.NET transaction. For a savepoint this is the transaction of the outermost scope.</summary>
        public SqliteTransaction Transaction { get; private set; }

        /// <summary>Name of the SAVEPOINT for a nested scope, null for the outermost one.</summary>
        public string SavepointName { get; private set; }

        /// <summary>True when this scope is nested inside another transaction scope.</summary>
        public bool IsSavepoint
        {
            get { return SavepointName != null; }
        }

        /// <summary>Requested transaction mode.</summary>
        public SQLiteTransactionMode Mode { get; private set; }

        /// <summary>When the transaction started.</summary>
        public DateTime StartedAtUtc { get; private set; }

        /// <summary>True until the transaction is committed or rolled back.</summary>
        public bool IsActive
        {
            get { return _completed == 0; }
        }

        internal static SQLiteTransactionHandle CreateRoot(SQLiteConnectionHandle handle, SqliteTransaction transaction, SQLiteTransactionMode mode)
        {
            return new SQLiteTransactionHandle(handle, transaction, null, null, mode);
        }

        internal static SQLiteTransactionHandle CreateSavepoint(SQLiteConnectionHandle handle, SQLiteTransactionHandle parent)
        {
            var name = "shaker_sp_" + Interlocked.Increment(ref _savepointCounter).ToString(CultureInfo.InvariantCulture);
            var savepoint = new SQLiteTransactionHandle(handle, parent.Transaction, parent, name, parent.Mode);
            savepoint.ExecuteControlStatement("SAVEPOINT " + name + ";");
            handle.CurrentTransaction = savepoint;
            return savepoint;
        }

        /// <summary>Commits this scope. For a nested scope the savepoint is released.</summary>
        public void Commit()
        {
            if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0)
            {
                return;
            }

            try
            {
                if (IsSavepoint)
                {
                    ExecuteControlStatement("RELEASE SAVEPOINT " + SavepointName + ";");
                }
                else
                {
                    Transaction.Commit();
                }
            }
            catch (SqliteException ex)
            {
                throw new SQLiteActivityException("Could not commit the SQLite transaction: " + ex.Message, ex);
            }
            finally
            {
                Detach();
            }
        }

        /// <summary>Rolls this scope back. For a nested scope only the work done since the savepoint is undone.</summary>
        public void Rollback()
        {
            if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0)
            {
                return;
            }

            try
            {
                if (IsSavepoint)
                {
                    ExecuteControlStatement("ROLLBACK TO SAVEPOINT " + SavepointName + ";");
                    ExecuteControlStatement("RELEASE SAVEPOINT " + SavepointName + ";");
                }
                else
                {
                    Transaction.Rollback();
                }
            }
            catch (SqliteException)
            {
                // A transaction that the engine already rolled back (for example after a constraint
                // failure with ON CONFLICT ROLLBACK) must not mask the original error.
            }
            finally
            {
                Detach();
            }
        }

        private void ExecuteControlStatement(string sql)
        {
            using (var command = _handle.Connection.CreateCommand())
            {
                command.Transaction = Transaction;
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        private void Detach()
        {
            if (ReferenceEquals(_handle.CurrentTransaction, this))
            {
                _handle.CurrentTransaction = _parent;
            }

            if (!IsSavepoint && Transaction != null)
            {
                Transaction.Dispose();
            }
        }

        /// <summary>Rolls the transaction back when it was not completed explicitly.</summary>
        public void Dispose()
        {
            Rollback();
        }
    }
}
