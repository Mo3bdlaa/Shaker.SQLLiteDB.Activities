using System;
using System.Runtime.Serialization;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.IO
{
    /// <summary>Raised when a result set does not fit into the limits of the XLSX format.</summary>
    [Serializable]
    public class XlsxLimitException : SQLiteActivityException
    {
        public XlsxLimitException() { }

        public XlsxLimitException(string message) : base(message) { }

        public XlsxLimitException(string message, Exception innerException) : base(message, innerException) { }

        protected XlsxLimitException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
