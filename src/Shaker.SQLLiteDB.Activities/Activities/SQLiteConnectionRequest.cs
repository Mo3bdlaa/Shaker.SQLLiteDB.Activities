using System;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>
    /// Describes how an activity gets its connection: either it reuses the one an enclosing scope opened,
    /// or it opens a private one for the duration of the activity.
    /// </summary>
    public sealed class SQLiteConnectionRequest
    {
        private readonly SQLiteConnectionHandle _existingHandle;
        private readonly SQLiteConnectionSettings _settings;

        private SQLiteConnectionRequest(SQLiteConnectionHandle existingHandle, SQLiteConnectionSettings settings, SQLiteLockToken ambientLock)
        {
            _existingHandle = existingHandle;
            _settings = settings;
            AmbientLock = ambientLock;
        }

        /// <summary>Writer lock held by an enclosing SQLite Write Lock Scope, if any.</summary>
        public SQLiteLockToken AmbientLock { get; private set; }

        /// <summary>True when the connection comes from an enclosing scope and must not be closed here.</summary>
        public bool UsesConnection
        {
            get { return _existingHandle != null; }
        }

        public static SQLiteConnectionRequest FromHandle(SQLiteConnectionHandle handle, SQLiteLockToken ambientLock)
        {
            if (handle == null)
            {
                throw new ArgumentNullException("handle");
            }

            return new SQLiteConnectionRequest(handle, null, ambientLock);
        }

        public static SQLiteConnectionRequest FromSettings(SQLiteConnectionSettings settings, SQLiteLockToken ambientLock)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            return new SQLiteConnectionRequest(null, settings, ambientLock);
        }

        /// <summary>Gets the connection to work with. Dispose the lease when done.</summary>
        public SQLiteConnectionLease Acquire()
        {
            if (_existingHandle != null)
            {
                if (!_existingHandle.IsOpen)
                {
                    throw new SQLiteActivityException(
                        "The SQLite connection handed over by the enclosing scope is already closed. " +
                        "Check that the activity really runs inside its SQLite Connect Scope.");
                }

                if (AmbientLock != null)
                {
                    _existingHandle.AmbientLock = AmbientLock;
                }

                return new SQLiteConnectionLease(_existingHandle, false);
            }

            var handle = SQLiteConnectionHandle.Open(_settings);
            if (AmbientLock != null)
            {
                handle.AmbientLock = AmbientLock;
            }

            return new SQLiteConnectionLease(handle, true);
        }
    }

    /// <summary>Borrowed connection. Disposing it closes the connection only when the activity opened it itself.</summary>
    public sealed class SQLiteConnectionLease : IDisposable
    {
        private readonly bool _ownsHandle;

        internal SQLiteConnectionLease(SQLiteConnectionHandle handle, bool ownsHandle)
        {
            Handle = handle;
            _ownsHandle = ownsHandle;
        }

        /// <summary>The connection to use.</summary>
        public SQLiteConnectionHandle Handle { get; private set; }

        public void Dispose()
        {
            if (_ownsHandle && Handle != null)
            {
                Handle.Dispose();
            }
        }
    }
}
