using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>
    /// Serializes writers to one database. Two layers are used:
    /// an in-process semaphore (cheap, covers parallel branches inside one robot) and an exclusively
    /// opened lock file (covers other processes, other robots and other machines).
    /// The operating system releases the file handle when a process dies, so a crashed robot cannot
    /// leave a stale lock behind.
    /// <para>
    /// A token may be released on a different thread than the one that took it, which is normal in a
    /// workflow: the scope takes the lock on one scheduler thread and releases it on another.
    /// </para>
    /// </summary>
    public static class SQLiteWriteLock
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> InProcessLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Takes the writer lock for <paramref name="databaseKey"/>, or throws
        /// <see cref="SQLiteLockTimeoutException"/> when it cannot be taken in time.
        /// The returned token must be disposed; disposing it releases the lock.
        /// </summary>
        public static SQLiteLockToken Acquire(string databaseKey, string lockFilePath, SQLiteLockOptions options, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                options = new SQLiteLockOptions();
            }

            if (options.Scope == SQLiteLockScope.None || string.IsNullOrEmpty(databaseKey))
            {
                return SQLiteLockToken.Disabled();
            }

            var timeout = options.AcquireTimeoutMilliseconds <= 0 ? Timeout.Infinite : options.AcquireTimeoutMilliseconds;
            var watch = Stopwatch.StartNew();
            var semaphore = InProcessLocks.GetOrAdd(databaseKey, _ => new SemaphoreSlim(1, 1));

            if (!semaphore.Wait(timeout, cancellationToken))
            {
                throw new SQLiteLockTimeoutException(string.Format(CultureInfo.InvariantCulture,
                    "Timed out after {0} ms waiting for the writer lock on '{1}'. Another activity in this process is still writing. " +
                    "Increase the lock timeout or shorten the write transaction.",
                    options.AcquireTimeoutMilliseconds, databaseKey));
            }

            FileStream fileLock = null;
            try
            {
                if (options.Scope == SQLiteLockScope.Machine && !string.IsNullOrEmpty(lockFilePath))
                {
                    var remaining = timeout == Timeout.Infinite
                        ? Timeout.Infinite
                        : Math.Max(0, timeout - (int)watch.ElapsedMilliseconds);

                    fileLock = AcquireFileLock(lockFilePath, remaining, options, databaseKey, cancellationToken);
                }

                return new SQLiteLockToken(databaseKey, lockFilePath, semaphore, fileLock, options, watch.ElapsedMilliseconds);
            }
            catch
            {
                if (fileLock != null)
                {
                    fileLock.Dispose();
                }

                semaphore.Release();
                throw;
            }
        }

        private static FileStream AcquireFileLock(string lockFilePath, int timeoutMilliseconds, SQLiteLockOptions options, string databaseKey, CancellationToken cancellationToken)
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(lockFilePath));
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var watch = Stopwatch.StartNew();
            var poll = options.PollIntervalMilliseconds <= 0 ? 50 : options.PollIntervalMilliseconds;
            var random = new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId);
            Exception lastError = null;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var stream = new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 64, FileOptions.WriteThrough);
                    if (options.WriteOwnerInfo)
                    {
                        WriteOwnerInfo(lockFilePath);
                    }

                    return stream;
                }
                catch (IOException ex)
                {
                    lastError = ex;
                }
                catch (UnauthorizedAccessException ex)
                {
                    // A folder we may not write to can never host a lock file: fail fast with a clear message.
                    throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                        "The writer lock file '{0}' cannot be created or opened. Grant write access to the folder, point " +
                        "'LockFilePath' at a writable location, or set the lock scope to Process or None.", lockFilePath), ex);
                }

                if (timeoutMilliseconds != Timeout.Infinite && watch.ElapsedMilliseconds >= timeoutMilliseconds)
                {
                    throw new SQLiteLockTimeoutException(string.Format(CultureInfo.InvariantCulture,
                        "Timed out after {0} ms waiting for the writer lock file '{1}' on database '{2}'. Current holder: {3}.",
                        timeoutMilliseconds, lockFilePath, databaseKey, ReadOwnerInfo(lockFilePath)), lastError);
                }

                var wait = poll + random.Next(0, Math.Max(1, poll));
                if (timeoutMilliseconds != Timeout.Infinite)
                {
                    wait = (int)Math.Min(wait, Math.Max(1, timeoutMilliseconds - watch.ElapsedMilliseconds));
                }

                if (cancellationToken.WaitHandle.WaitOne(wait))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        private static void WriteOwnerInfo(string lockFilePath)
        {
            try
            {
                var text = string.Format(CultureInfo.InvariantCulture,
                    "machine={0};user={1};process={2};pid={3};sinceUtc={4:O}",
                    Environment.MachineName,
                    Environment.UserName,
                    GetProcessName(),
                    GetProcessId(),
                    DateTime.UtcNow);

                using (var stream = new FileStream(lockFilePath + ".owner", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(text);
                }
            }
            catch
            {
                // Diagnostics only.
            }
        }

        internal static string ReadOwnerInfo(string lockFilePath)
        {
            try
            {
                var path = lockFilePath + ".owner";
                if (!File.Exists(path))
                {
                    return "unknown";
                }

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    return string.IsNullOrWhiteSpace(text) ? "unknown" : text.Trim();
                }
            }
            catch
            {
                return "unknown";
            }
        }

        private static int GetProcessId()
        {
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    return process.Id;
                }
            }
            catch
            {
                return -1;
            }
        }

        private static string GetProcessName()
        {
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    return process.ProcessName;
                }
            }
            catch
            {
                return "unknown";
            }
        }

    }
}
