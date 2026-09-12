using System;
using System.Globalization;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>Retry behaviour for the transient "database is locked" / "database is busy" errors.</summary>
    public class SQLiteRetryOptions
    {
        /// <summary>Total number of attempts, including the first one. One means no retry. Default 5.</summary>
        public int MaxAttempts { get; set; } = 5;

        /// <summary>Delay before the second attempt. Default 50 ms.</summary>
        public int InitialDelayMilliseconds { get; set; } = 50;

        /// <summary>Multiplier applied to the delay after every failed attempt. Default 2.</summary>
        public double BackoffFactor { get; set; } = 2.0;

        /// <summary>Upper bound of the delay between two attempts. Default 2 seconds.</summary>
        public int MaxDelayMilliseconds { get; set; } = 2000;

        public SQLiteRetryOptions Clone()
        {
            return new SQLiteRetryOptions
            {
                MaxAttempts = MaxAttempts,
                InitialDelayMilliseconds = InitialDelayMilliseconds,
                BackoffFactor = BackoffFactor,
                MaxDelayMilliseconds = MaxDelayMilliseconds
            };
        }
    }

    /// <summary>Runs a unit of work and retries it while SQLite reports a transient lock error.</summary>
    public static class SQLiteRetryPolicy
    {
        private const int SqliteBusy = 5;
        private const int SqliteLocked = 6;
        private const int SqliteProtocol = 15;

        /// <summary>True when the error is one that usually disappears if the caller simply tries again.</summary>
        public static bool IsTransient(Exception exception)
        {
            var sqliteException = exception as SqliteException;
            if (sqliteException == null)
            {
                return false;
            }

            // The low byte carries the primary result code, the high bytes carry the extended code.
            var primary = sqliteException.SqliteErrorCode & 0xFF;
            return primary == SqliteBusy || primary == SqliteLocked || primary == SqliteProtocol;
        }

        public static T Execute<T>(Func<T> work, SQLiteRetryOptions options, CancellationToken cancellationToken)
        {
            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            if (options == null)
            {
                options = new SQLiteRetryOptions();
            }

            var attempts = Math.Max(1, options.MaxAttempts);
            var delay = Math.Max(1, options.InitialDelayMilliseconds);
            var factor = options.BackoffFactor <= 0 ? 1.0 : options.BackoffFactor;
            var maxDelay = Math.Max(delay, options.MaxDelayMilliseconds);
            var random = new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId);

            for (var attempt = 1; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return work();
                }
                catch (Exception ex) when (IsTransient(ex) && attempt < attempts)
                {
                    var jitter = random.Next(0, Math.Max(1, delay / 2));
                    var wait = Math.Min(maxDelay, delay) + jitter;

                    if (cancellationToken.WaitHandle.WaitOne(wait))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    delay = (int)Math.Min(maxDelay, delay * factor);
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    throw new SQLiteActivityException(string.Format(CultureInfo.InvariantCulture,
                        "SQLite is still reporting '{0}' after {1} attempts. Another writer is holding the database. " +
                        "Increase 'BusyTimeout' or the retry count, use a SQLite Write Lock Scope around the write, " +
                        "or shorten the competing transaction.", ex.Message, attempts), ex);
                }
            }
        }

        public static void Execute(Action work, SQLiteRetryOptions options, CancellationToken cancellationToken)
        {
            Execute<object>(() => { work(); return null; }, options, cancellationToken);
        }
    }
}
