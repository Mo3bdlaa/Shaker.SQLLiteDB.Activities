using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Shaker.SQLLiteDB.Activities.Activities;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Tests
{
    /// <summary>Hands a value from inside a workflow back to the test.</summary>
    public class Capture<T> : CodeActivity
    {
        public InArgument<T> Value { get; set; }

        public Action<T> OnValue { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            OnValue(Value.Get(context));
        }
    }

    /// <summary>Runs an action on the workflow thread, for checks in the middle of a scope.</summary>
    public class Inspect : CodeActivity
    {
        public Action Action { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            Action();
        }
    }

    /// <summary>Always throws, to test rollback and ContinueOnError.</summary>
    public class Boom : CodeActivity
    {
        public string Message { get; set; } = "boom";

        protected override void Execute(CodeActivityContext context)
        {
            throw new InvalidOperationException(Message);
        }
    }

    internal static class WorkflowHelpers
    {
        /// <summary>Wraps activities in a SQLite Connect Scope on the given database.</summary>
        public static SQLiteConnectScope ConnectScope(string databasePath, params Activity[] activities)
        {
            var sequence = new Sequence();
            foreach (var activity in activities)
            {
                sequence.Activities.Add(activity);
            }

            return new SQLiteConnectScope
            {
                DatabasePath = databasePath,
                Body = new ActivityAction<SQLiteConnectionHandle>
                {
                    Argument = new DelegateInArgument<SQLiteConnectionHandle>("SQLiteConnection"),
                    Handler = sequence
                }
            };
        }

        public static SQLiteTransactionScope TransactionScope(params Activity[] activities)
        {
            var sequence = new Sequence();
            foreach (var activity in activities)
            {
                sequence.Activities.Add(activity);
            }

            return new SQLiteTransactionScope
            {
                Body = new ActivityAction<SQLiteConnectionHandle>
                {
                    Argument = new DelegateInArgument<SQLiteConnectionHandle>("SQLiteConnection"),
                    Handler = sequence
                }
            };
        }

        public static SQLiteWriteLockScope WriteLockScope(params Activity[] activities)
        {
            var sequence = new Sequence();
            foreach (var activity in activities)
            {
                sequence.Activities.Add(activity);
            }

            return new SQLiteWriteLockScope
            {
                Body = new ActivityAction<SQLiteLockToken>
                {
                    Argument = new DelegateInArgument<SQLiteLockToken>("SQLiteWriteLock"),
                    Handler = sequence
                }
            };
        }

        public static IDictionary<string, object> Run(Activity activity)
        {
            return WorkflowInvoker.Invoke(activity);
        }
    }
}
