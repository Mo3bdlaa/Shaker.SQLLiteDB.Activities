using System;
using System.Activities;
using System.ComponentModel;
using System.Threading;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Connection
{
    /// <summary>
    /// Holds the writer lock for everything inside it. Use it when several write activities belong
    /// together, so that no other process can slip a write in between, and so that the lock is taken
    /// once instead of once per activity.
    /// </summary>
    [Category("SQLite")]
    [DisplayName("SQLite Write Lock Scope")]
    [Description("Takes the cross process writer lock once and keeps it for all the activities inside.")]
#if NET6_0_OR_GREATER
    [System.Activities.ViewModels.ViewModelClass(typeof(Shaker.SQLLiteDB.Activities.Design.SQLiteWriteLockScopeViewModel))]
#endif
    public class SQLiteWriteLockScope : NativeActivity
    {
        private readonly Variable<SQLiteLockToken> _token = new Variable<SQLiteLockToken>("ShakerSQLiteWriteLock");

        public SQLiteWriteLockScope()
        {
            DisplayName = "SQLite Write Lock Scope";
            Body = new ActivityAction<SQLiteLockToken>
            {
                Argument = new DelegateInArgument<SQLiteLockToken>("SQLiteWriteLock")
            };
        }

        /// <summary>Activities that run while the lock is held.</summary>
        [Browsable(false)]
        public ActivityAction<SQLiteLockToken> Body { get; set; }

        [Category("Connection")]
        [DisplayName("Connection")]
        [Description("Connection whose database should be locked. Leave empty inside a SQLite Connect Scope.")]
        public InArgument<SQLiteConnectionHandle> Connection { get; set; }

        [Category("Connection")]
        [DisplayName("Database path")]
        [Description("Database to lock, when the scope is used without a connection.")]
        public InArgument<string> DatabasePath { get; set; }

        [Category("Locking")]
        [DisplayName("Lock scope")]
        [Description("Machine: a lock file serializes writers across processes and machines. Process: only inside this robot.")]
        public SQLiteLockScope LockScope { get; set; } = SQLiteLockScope.Machine;

        [Category("Locking")]
        [DisplayName("Lock file path")]
        [Description("Lock file to use. Empty uses '<database file>.writelock'.")]
        public InArgument<string> LockFilePath { get; set; }

        [Category("Locking")]
        [DisplayName("Lock timeout (ms)")]
        [Description("How long the scope waits for the lock before it fails.")]
        public InArgument<int> LockTimeoutMilliseconds { get; set; } = 60000;

        [Category("Output")]
        [DisplayName("Waited (ms)")]
        [Description("How long this scope had to wait before it got the lock.")]
        public OutArgument<long> WaitedMilliseconds { get; set; }

        protected override bool CanInduceIdle
        {
            get { return true; }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            metadata.AddImplementationVariable(_token);
        }

        protected override void Execute(NativeActivityContext context)
        {
            var handle = (Connection == null ? null : Connection.Get(context))
                         ?? SQLiteExecutionProperties.FindConnection(context);

            var databasePath = DatabasePath == null ? null : DatabasePath.Get(context);
            string databaseKey;

            var options = new SQLiteLockOptions
            {
                Scope = LockScope,
                LockFilePath = LockFilePath == null ? null : LockFilePath.Get(context),
                AcquireTimeoutMilliseconds = LockTimeoutMilliseconds == null ? 60000 : LockTimeoutMilliseconds.Get(context)
            };

            if (handle != null)
            {
                databaseKey = handle.Settings.DatabaseKey;
                databasePath = handle.DatabasePath;
            }
            else if (!string.IsNullOrWhiteSpace(databasePath))
            {
                var settings = new SQLiteConnectionSettings { DatabasePath = databasePath };
                databaseKey = settings.DatabaseKey;
                databasePath = settings.ResolvedDatabasePath;
            }
            else
            {
                throw new SQLiteActivityException(
                    "SQLite Write Lock Scope needs a database. Put it inside a SQLite Connect Scope or fill in 'Database path'.");
            }

            var token = SQLiteWriteLock.Acquire(databaseKey, options.ResolveLockFilePath(databasePath), options, CancellationToken.None);
            _token.Set(context, token);

            if (WaitedMilliseconds != null)
            {
                WaitedMilliseconds.Set(context, token.WaitedMilliseconds);
            }

            context.Properties.Add(SQLiteExecutionProperties.WriteLock, token);

            if (handle != null)
            {
                handle.AmbientLock = token;
            }

            if (Body == null)
            {
                Release(context);
                return;
            }

            context.ScheduleAction(Body, token, OnBodyCompleted, OnBodyFaulted);
        }

        private void OnBodyCompleted(NativeActivityContext context, ActivityInstance instance)
        {
            Release(context);
        }

        private void OnBodyFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            Release(faultContext);
        }

        private void Release(NativeActivityContext context)
        {
            var token = _token.Get(context);
            if (token == null)
            {
                return;
            }

            _token.Set(context, null);

            var handle = (Connection == null ? null : Connection.Get(context))
                         ?? SQLiteExecutionProperties.FindConnection(context);

            if (handle != null && ReferenceEquals(handle.AmbientLock, token))
            {
                handle.AmbientLock = null;
            }

            token.Dispose();
        }

        protected override void Cancel(NativeActivityContext context)
        {
            base.Cancel(context);
            Release(context);
        }

        protected override void Abort(NativeActivityAbortContext context)
        {
            var token = _token.Get(context);
            if (token != null)
            {
                token.Dispose();
            }

            base.Abort(context);
        }
    }
}
