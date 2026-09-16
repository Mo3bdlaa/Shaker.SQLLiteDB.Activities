using System;
using System.Activities;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>
    /// Base class of every SQLite activity that produces a value.
    /// <para>
    /// The activity itself is a NativeActivity, which is what lets it pick up the connection published by
    /// an enclosing SQLite Connect Scope. The database work is handed to a background worker, so the
    /// workflow thread stays free and parallel branches really do read in parallel.
    /// </para>
    /// </summary>
    public abstract class SQLiteActivityBase<TResult> : NativeActivity<TResult>
    {
        private readonly Variable<SQLiteWorkPlan<TResult>> _plan = new Variable<SQLiteWorkPlan<TResult>>("ShakerSQLitePlan");
        private readonly SQLiteWorker<TResult> _worker = new SQLiteWorker<TResult>();

        #region Connection

        /// <summary>Connection opened by a SQLite Connect Scope. Leave empty when the activity runs on its own.</summary>
        [Category("Connection")]
        [DisplayName("Connection")]
        [Description("Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead.")]
        public InArgument<SQLiteConnectionHandle> Connection { get; set; }

        /// <summary>Path of the database file, used when no scope provides a connection.</summary>
        [Category("Connection")]
        [DisplayName("Database path")]
        [Description("Full path of the SQLite database file, for example C:\\Data\\orders.db. Ignored when a connection or a connection string is supplied.")]
        public InArgument<string> DatabasePath { get; set; }

        /// <summary>Complete ADO.NET connection string, for full control.</summary>
        [Category("Connection")]
        [DisplayName("Connection string")]
        [Description("Complete connection string, for example \"Data Source=C:\\Data\\orders.db;Mode=ReadWriteCreate\". Wins over 'Database path'.")]
        public InArgument<string> ConnectionString { get; set; }

        /// <summary>How the database file is opened when this activity opens its own connection.</summary>
        [Category("Connection")]
        [DisplayName("Open mode")]
        [Description("How the database file is opened when this activity opens its own connection.")]
        public SQLiteOpenMode OpenMode { get; set; } = SQLiteOpenMode.ReadWriteCreate;

        /// <summary>Journal mode applied when this activity opens its own connection.</summary>
        [Category("Connection")]
        [DisplayName("Journal mode")]
        [Description("WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting.")]
        public SQLiteJournalMode JournalMode { get; set; } = SQLiteJournalMode.Wal;

        /// <summary>How long the engine itself waits for a lock before reporting SQLITE_BUSY.</summary>
        [Category("Connection")]
        [DisplayName("Busy timeout (ms)")]
        [Description("How long SQLite waits for a lock held by another connection before it reports 'database is locked'.")]
        public InArgument<int> BusyTimeoutMilliseconds { get; set; } = 30000;

        /// <summary>Password of an encrypted (SQLCipher) database.</summary>
        [Category("Connection")]
        [DisplayName("Password")]
        [Description("Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password.")]
        public InArgument<string> Password { get; set; }

        #endregion

        #region Locking

        /// <summary>How far the writer lock reaches.</summary>
        [Category("Locking")]
        [DisplayName("Lock scope")]
        [Description("Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone.")]
        public SQLiteLockScope LockScope { get; set; } = SQLiteLockScope.Machine;

        /// <summary>Lock file to use. Empty means &lt;database&gt;.writelock.</summary>
        [Category("Locking")]
        [DisplayName("Lock file path")]
        [Description("Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them.")]
        public InArgument<string> LockFilePath { get; set; }

        /// <summary>How long a writer waits for the lock before the activity fails.</summary>
        [Category("Locking")]
        [DisplayName("Lock timeout (ms)")]
        [Description("How long this activity waits for the writer lock before it fails.")]
        public InArgument<int> LockTimeoutMilliseconds { get; set; } = 60000;

        #endregion

        #region Common

        /// <summary>Maximum run time of the activity in milliseconds. Zero or less means no limit.</summary>
        [Category("Common")]
        [DisplayName("TimeoutMS")]
        [Description("Maximum run time of this activity in milliseconds. Use 0 for no limit.")]
        public InArgument<int> TimeoutMS { get; set; } = 120000;

        /// <summary>Continue the workflow when the activity fails.</summary>
        [Category("Common")]
        [DisplayName("ContinueOnError")]
        [Description("When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'.")]
        public InArgument<bool> ContinueOnError { get; set; }

        /// <summary>Message of the error that was swallowed because ContinueOnError was set.</summary>
        [Category("Output")]
        [DisplayName("Error message")]
        [Description("Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded.")]
        public OutArgument<string> ErrorMessage { get; set; }

        #endregion

        /// <summary>Work to run in the background plus the code that copies extra values into output arguments.</summary>
        protected sealed class SQLiteRun
        {
            /// <summary>The database work. It runs on a background thread and must not touch the workflow context.</summary>
            public Func<CancellationToken, TResult> Work { get; set; }

            /// <summary>Copies additional values into the output arguments once the work is done.</summary>
            public Action<NativeActivityContext, TResult> ApplyOutputs { get; set; }
        }

        /// <summary>
        /// Reads the arguments of the activity (this runs on the workflow thread) and returns the work
        /// that should be carried out in the background.
        /// </summary>
        protected abstract SQLiteRun CreateRun(NativeActivityContext context);

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            metadata.AddImplementationVariable(_plan);
            _worker.Plan = new InArgument<SQLiteWorkPlan<TResult>>(_plan);
            metadata.AddImplementationChild(_worker);
        }

        protected override void Execute(NativeActivityContext context)
        {
            SetValue(ErrorMessage, context, string.Empty);

            var timeout = GetValue(TimeoutMS, context, 0);
            SQLiteRun run;

            try
            {
                run = CreateRun(context);

                if (run == null || run.Work == null)
                {
                    throw new SQLiteActivityException("The activity did not produce any work to run.");
                }
            }
            catch (Exception ex)
            {
                // Reading the arguments failed. Report it through the worker so that ContinueOnError
                // treats configuration problems and runtime problems the same way.
                var failure = ex;
                run = new SQLiteRun { Work = cancellationToken => { throw failure; } };
            }

            _plan.Set(context, new SQLiteWorkPlan<TResult>
            {
                Work = run.Work,
                ApplyOutputs = run.ApplyOutputs,
                TimeoutMilliseconds = timeout,
                ActivityName = DisplayName
            });

            context.ScheduleActivity(_worker, OnWorkerCompleted, OnWorkerFaulted);
        }

        private void OnWorkerCompleted(NativeActivityContext context, ActivityInstance instance, TResult result)
        {
            Result.Set(context, result);

            // The plan lives in an implementation variable, so each execution of this activity has its
            // own copy: parallel branches never overwrite each other's outputs.
            var plan = _plan.Get(context);

            if (plan != null && plan.ApplyOutputs != null)
            {
                plan.ApplyOutputs(context, result);
            }
        }

        private void OnWorkerFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            if (!GetValue(ContinueOnError, faultContext, false))
            {
                return;
            }

            faultContext.CancelChild(propagatedFrom);
            faultContext.HandleFault();

            SetValue(ErrorMessage, faultContext, propagatedException.Message);
            Result.Set(faultContext, default(TResult));
        }

        #region Helpers for derived activities

        /// <summary>Decides which connection this activity works on.</summary>
        protected SQLiteConnectionRequest ResolveConnection(NativeActivityContext context)
        {
            var ambientLock = SQLiteExecutionProperties.FindWriteLock(context);
            var handle = GetValue(Connection, context, null) ?? SQLiteExecutionProperties.FindConnection(context);

            if (handle != null)
            {
                return SQLiteConnectionRequest.FromHandle(handle, ambientLock);
            }

            return SQLiteConnectionRequest.FromSettings(BuildSettings(context), ambientLock);
        }

        /// <summary>Builds the connection settings from the properties of this activity.</summary>
        protected SQLiteConnectionSettings BuildSettings(NativeActivityContext context)
        {
            var connectionString = GetValue(ConnectionString, context, null);
            var databasePath = GetValue(DatabasePath, context, null);

            if (string.IsNullOrWhiteSpace(connectionString) && string.IsNullOrWhiteSpace(databasePath))
            {
                throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                    "Activity '{0}' has no database to work on. Put it inside a SQLite Connect Scope, or fill in 'Database path' or 'Connection string'.",
                    DisplayName));
            }

            return new SQLiteConnectionSettings
            {
                DatabasePath = databasePath,
                ConnectionString = connectionString,
                OpenMode = OpenMode,
                JournalMode = JournalMode,
                Password = GetValue(Password, context, null),
                BusyTimeoutMilliseconds = GetValue(BusyTimeoutMilliseconds, context, 30000),
                Lock = new SQLiteLockOptions
                {
                    Scope = LockScope,
                    LockFilePath = GetValue(LockFilePath, context, null),
                    AcquireTimeoutMilliseconds = GetValue(LockTimeoutMilliseconds, context, 60000)
                }
            };
        }

        /// <summary>Reads an argument that the user may have left unconfigured.</summary>
        protected static T GetValue<T>(InArgument<T> argument, ActivityContext context, T fallback)
        {
            return argument == null ? fallback : argument.Get(context);
        }

        /// <summary>Writes an output argument that the user may have left unconfigured.</summary>
        protected static void SetValue<T>(OutArgument<T> argument, ActivityContext context, T value)
        {
            if (argument != null)
            {
                argument.Set(context, value);
            }
        }

        #endregion
    }
}
