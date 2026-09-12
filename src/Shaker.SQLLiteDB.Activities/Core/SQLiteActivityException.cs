using System;
using System.Runtime.Serialization;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>Error raised by the Shaker SQLite activities.</summary>
    [Serializable]
    public class SQLiteActivityException : Exception
    {
        public SQLiteActivityException() { }

        public SQLiteActivityException(string message) : base(message) { }

        public SQLiteActivityException(string message, Exception innerException) : base(message, innerException) { }

        protected SQLiteActivityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>Raised when the writer lock could not be taken before the configured timeout elapsed.</summary>
    [Serializable]
    public class SQLiteLockTimeoutException : SQLiteActivityException
    {
        public SQLiteLockTimeoutException() { }

        public SQLiteLockTimeoutException(string message) : base(message) { }

        public SQLiteLockTimeoutException(string message, Exception innerException) : base(message, innerException) { }

        protected SQLiteLockTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
