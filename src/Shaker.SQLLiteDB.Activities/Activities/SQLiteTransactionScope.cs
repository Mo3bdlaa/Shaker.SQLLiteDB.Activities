using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Connection
{
    /// <summary>
    /// Runs the activities inside one transaction: everything is committed together, or nothing is.
    /// Nested transaction scopes use a SAVEPOINT, so an inner scope can fail without discarding the outer work.
    /// </summary>
    [Category("SQLite")]
    [DisplayName("SQLite Transaction Scope")]
    [Description("Commits the activities inside as one unit of work and rolls everything back when one of them fails.")]
#if NET6_0_OR_GREATER
    [System.Activities.ViewModels.ViewModelClass(typeof(Shaker.SQLLiteDB.Activities.Design.SQLiteTransactionScopeViewModel))]
#endif
    public class SQLiteTransactionScope : NativeActivity
    {
        private readonly Variable<SQLiteTransactionHandle> _transaction = new Variable<SQLiteTransactionHandle>("ShakerSQLiteTransaction");
        private readonly Variable<SQLiteLockToken> _lock = new Variable<SQLiteLockToken>("ShakerSQLiteTransactionLock");

        public SQLiteTransactionScope()
        {
            DisplayName = "SQLite Transaction Scope";
            Body = new ActivityAction<SQLiteConnectionHandle>
            {
                Argument = new DelegateInArgument<SQLiteConnectionHandle>("SQLiteConnection"),

                // A scope holds one activity, so an empty one would let you drop a single activity in
                // and nothing more. Starting with a Sequence is what makes it behave the way every
                // other scope in Studio does. Opening an existing workflow replaces this with whatever
                // the file says, so it only ever applies to a scope you have just dragged in.
                Handler = new Sequence { DisplayName = "Do" }
            };
        }

        /// <summary>Activities that run inside the transaction.</summary>
        [Browsable(false)]
        public ActivityAction<SQLiteConnectionHandle> Body { get; set; }

        [Category("Connection")]
        [DisplayName("Connection")]
        [Description("Connection to work on. Leave empty when the scope sits inside a SQLite Connect Scope.")]
        public InArgument<SQLiteConnectionHandle> Connection { get; set; }

        [Category("Transaction")]
        [DisplayName("Transaction mode")]
        [Description("Immediate takes the write lock right away and is the safer choice when several robots write. Deferred waits for the first write.")]
        public SQLiteTransactionMode TransactionMode { get; set; } = SQLiteTransactionMode.Immediate;

        [Category("Transaction")]
        [DisplayName("Take writer lock")]
        [Description("Hold the writer lock for the whole transaction so that other processes queue up instead of failing with 'database is locked'.")]
        public bool TakeWriteLock { get; set; } = true;

        [Category("Transaction")]
        [DisplayName("Lock timeout (ms)")]
        [Description("How long the scope waits for the writer lock before it fails.")]
        public InArgument<int> LockTimeoutMilliseconds { get; set; } = 60000;

        [Category("Output")]
        [DisplayName("Committed")]
        [Description("True when the transaction was committed, false when it was rolled back.")]
        public OutArgument<bool> Committed { get; set; }

        protected override bool CanInduceIdle
        {
            get { return true; }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            metadata.AddImplementationVariable(_transaction);
            metadata.AddImplementationVariable(_lock);
        }

        protected override void Execute(NativeActivityContext context)
        {
            var handle = (Connection == null ? null : Connection.Get(context))
                         ?? SQLiteExecutionProperties.FindConnection(context);

            if (handle == null)
            {
                throw new SQLiteActivityException(
                    "SQLite Transaction Scope needs an open connection. Put it inside a SQLite Connect Scope or set 'Existing connection'.");
            }

            if (Committed != null)
            {
                Committed.Set(context, false);
            }

            if (TakeWriteLock && !handle.Settings.IsReadOnly)
            {
                var lockOptions = handle.Settings.Lock == null ? new SQLiteLockOptions() : handle.Settings.Lock.Clone();
                if (LockTimeoutMilliseconds != null)
                {
                    lockOptions.AcquireTimeoutMilliseconds = LockTimeoutMilliseconds.Get(context);
                }

                var ambient = SQLiteExecutionProperties.FindWriteLock(context);
                var token = ambient != null
                    ? ambient
                    : SQLiteWriteLock.Acquire(handle.Settings.DatabaseKey, lockOptions.ResolveLockFilePath(handle.DatabasePath), lockOptions, System.Threading.CancellationToken.None);

                if (ambient == null)
                {
                    _lock.Set(context, token);
                    context.Properties.Add(SQLiteExecutionProperties.WriteLock, token);
                }

                handle.AmbientLock = token;
            }

            SQLiteTransactionHandle transaction;

            try
            {
                transaction = handle.BeginTransaction(TransactionMode);
            }
            catch
            {
                ReleaseLock(context, handle);
                throw;
            }

            _transaction.Set(context, transaction);

            if (Body == null)
            {
                Complete(context, handle, true);
                return;
            }

            context.ScheduleAction(Body, handle, OnBodyCompleted, OnBodyFaulted);
        }

        private void OnBodyCompleted(NativeActivityContext context, ActivityInstance instance)
        {
            var handle = (Connection == null ? null : Connection.Get(context))
                         ?? SQLiteExecutionProperties.FindConnection(context);
            Complete(context, handle, true);
        }

        private void OnBodyFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            var handle = (Connection == null ? null : Connection.Get(faultContext))
                         ?? SQLiteExecutionProperties.FindConnection(faultContext);
            Complete(faultContext, handle, false);
        }

        private void Complete(NativeActivityContext context, SQLiteConnectionHandle handle, bool commit)
        {
            var transaction = _transaction.Get(context);
            _transaction.Set(context, null);

            try
            {
                if (transaction != null && transaction.IsActive)
                {
                    if (commit)
                    {
                        transaction.Commit();
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }

                if (Committed != null)
                {
                    Committed.Set(context, commit);
                }
            }
            finally
            {
                ReleaseLock(context, handle);
            }
        }

        private void ReleaseLock(NativeActivityContext context, SQLiteConnectionHandle handle)
        {
            var token = _lock.Get(context);
            if (token == null)
            {
                return;
            }

            _lock.Set(context, null);

            if (handle != null && ReferenceEquals(handle.AmbientLock, token))
            {
                handle.AmbientLock = null;
            }

            token.Dispose();
        }

        protected override void Cancel(NativeActivityContext context)
        {
            base.Cancel(context);

            var handle = (Connection == null ? null : Connection.Get(context))
                         ?? SQLiteExecutionProperties.FindConnection(context);
            Complete(context, handle, false);
        }

        protected override void Abort(NativeActivityAbortContext context)
        {
            var transaction = _transaction.Get(context);
            if (transaction != null && transaction.IsActive)
            {
                transaction.Rollback();
            }

            var token = _lock.Get(context);
            if (token != null)
            {
                token.Dispose();
            }

            base.Abort(context);
        }
    }
}
