using System;
using System.Activities;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>Names of the execution properties that the scope activities publish to their children.</summary>
    internal static class SQLiteExecutionProperties
    {
        /// <summary>Connection published by a SQLite Connect Scope.</summary>
        public const string Connection = "Shaker.SQLLiteDB.Connection";

        /// <summary>Writer lock published by a SQLite Write Lock Scope.</summary>
        public const string WriteLock = "Shaker.SQLLiteDB.WriteLock";

        public static SQLiteConnectionHandle FindConnection(CodeActivityContext context)
        {
            return Find(context, Connection) as SQLiteConnectionHandle;
        }

        public static SQLiteConnectionHandle FindConnection(NativeActivityContext context)
        {
            return context == null ? null : FindConnection(context.Properties);
        }

        public static SQLiteLockToken FindWriteLock(CodeActivityContext context)
        {
            var token = Find(context, WriteLock) as SQLiteLockToken;
            return token != null && token.IsHeld ? token : null;
        }

        /// <summary>
        /// Reads an execution property published by an enclosing scope. A CodeActivity cannot touch
        /// ExecutionProperties directly, so the value is read through the data context, which is the
        /// way UiPath scopes hand state to the activities inside them.
        /// </summary>
        private static object Find(ActivityContext context, string tag)
        {
            if (context == null)
            {
                return null;
            }

            try
            {
                var dataContext = context.DataContext;
                if (dataContext == null)
                {
                    return null;
                }

                var descriptor = dataContext.GetProperties()[tag];
                return descriptor == null ? null : descriptor.GetValue(dataContext);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public static SQLiteLockToken FindWriteLock(NativeActivityContext context)
        {
            return context == null ? null : FindWriteLock(context.Properties);
        }

        private static SQLiteConnectionHandle FindConnection(ExecutionProperties properties)
        {
            try
            {
                return properties.Find(Connection) as SQLiteConnectionHandle;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static SQLiteLockToken FindWriteLock(ExecutionProperties properties)
        {
            try
            {
                var token = properties.Find(WriteLock) as SQLiteLockToken;
                return token != null && token.IsHeld ? token : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
