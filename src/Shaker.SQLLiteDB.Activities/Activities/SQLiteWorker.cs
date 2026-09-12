using System;
using System.Activities;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>The database work that an activity shell hands to its background worker.</summary>
    internal sealed class SQLiteWorkPlan<TResult>
    {
        public Func<CancellationToken, TResult> Work { get; set; }

        public int TimeoutMilliseconds { get; set; }

        public string ActivityName { get; set; }

        /// <summary>Copies extra values into the output arguments once the work is done.</summary>
        public Action<NativeActivityContext, TResult> ApplyOutputs { get; set; }
    }

    /// <summary>
    /// Runs the work of a SQLite activity on a background thread. Being an AsyncCodeActivity it hands
    /// the workflow thread back immediately, so parallel branches really do run at the same time.
    /// </summary>
    internal sealed class SQLiteWorker<TResult> : AsyncCodeActivity<TResult>
    {
        public InArgument<SQLiteWorkPlan<TResult>> Plan { get; set; }

        private sealed class RunState
        {
            public Task<TResult> Task { get; set; }

            public CancellationTokenSource Cancellation { get; set; }

            public SQLiteWorkPlan<TResult> Plan { get; set; }
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var plan = Plan.Get(context);
            var cancellation = new CancellationTokenSource();

            if (plan.TimeoutMilliseconds > 0)
            {
                cancellation.CancelAfter(plan.TimeoutMilliseconds);
            }

            var token = cancellation.Token;
            var work = plan.Work;
            var task = Task.Factory.StartNew(() => work(token), token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            context.UserState = new RunState { Task = task, Cancellation = cancellation, Plan = plan };

            var source = new TaskCompletionSource<object>(state);

            task.ContinueWith(completed =>
            {
                if (completed.IsFaulted)
                {
                    source.TrySetException(completed.Exception.InnerExceptions);

                    // The error is reported from the typed task in EndExecute; mark this one observed.
                    GC.KeepAlive(source.Task.Exception);
                }
                else if (completed.IsCanceled)
                {
                    source.TrySetCanceled();
                    GC.KeepAlive(source.Task.IsCanceled);
                }
                else
                {
                    source.TrySetResult(null);
                }

                if (callback != null)
                {
                    callback(source.Task);
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return source.Task;
        }

        protected override TResult EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var state = (RunState)context.UserState;

            try
            {
                try
                {
                    return state.Task.GetAwaiter().GetResult();
                }
                catch (AggregateException ex)
                {
                    throw Translate(ex.InnerException ?? ex, state.Plan);
                }
                catch (Exception ex)
                {
                    throw Translate(ex, state.Plan);
                }
            }
            finally
            {
                state.Cancellation.Dispose();
            }
        }

        protected override void Cancel(AsyncCodeActivityContext context)
        {
            var state = context.UserState as RunState;
            if (state != null)
            {
                state.Cancellation.Cancel();
            }

            base.Cancel(context);
        }

        private static Exception Translate(Exception exception, SQLiteWorkPlan<TResult> plan)
        {
            if (exception is OperationCanceledException && plan != null && plan.TimeoutMilliseconds > 0)
            {
                return new TimeoutException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}' was stopped after {1} ms (TimeoutMS). Raise TimeoutMS, or make the statement faster.",
                    plan.ActivityName, plan.TimeoutMilliseconds), exception);
            }

            return exception;
        }
    }
}
