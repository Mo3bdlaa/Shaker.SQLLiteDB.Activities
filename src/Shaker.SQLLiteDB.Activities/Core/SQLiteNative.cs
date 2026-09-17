using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Shaker.SQLLiteDB.Activities.Core
{
    /// <summary>
    /// Bootstraps the embedded SQLite engine (SQLitePCLRaw / SQLCipher) that ships inside this package.
    /// No ODBC driver, no System.Data.SQLite installation and no registry entry is needed on the robot,
    /// and because the engine is a SQLCipher build, AES-256 encrypted databases work out of the box.
    /// </summary>
    public static class SQLiteNative
    {
        private static int _initialized;
        private static string _version;
        private static string _cipherVersion;

        /// <summary>
        /// Loads the bundled native SQLite library. Safe to call from several threads and as often as you like.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            {
                return;
            }

            var probed = new List<string>();
            try
            {
                PreloadWindowsEngine(probed);
                SQLitePCL.Batteries_V2.Init();
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _initialized, 0);
                throw new SQLiteActivityException(
                    "The embedded SQLite engine (e_sqlcipher) could not be loaded." +
                    (probed.Count == 0 ? string.Empty : " Looked for it in: " + string.Join("; ", probed.ToArray()) + "."),
                    ex);
            }
        }

        /// <summary>
        /// Loads the native engine that ships in this package by full path, before the provider's own
        /// DllImport gets a chance to fail.
        /// <para>
        /// A robot does not run the workflow from a build output folder: the activity assembly is loaded
        /// straight out of the extracted package, and .NET's plain <c>DllImport("e_sqlcipher")</c> does
        /// not search there. Loading the file by full path first puts the module in the process under the
        /// name the provider asks for, so its DllImport then resolves to it. Every layout the package can
        /// end up in is tried, which is why the list is this long.
        /// </para>
        /// </summary>
        private static void PreloadWindowsEngine(List<string> probed)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                // Linux and macOS: the provider finds its own native through the runtimes folder that
                // NuGet lays out, which is how the test suite runs.
                return;
            }

            var architecture = IntPtr.Size == 8 ? "x64" : "x86";
            var relative = new[]
            {
                "e_sqlcipher.dll",
                Path.Combine(architecture, "e_sqlcipher.dll"),
                Path.Combine("runtimes", "win-" + architecture, "native", "e_sqlcipher.dll"),
                Path.Combine("..", "..", "runtimes", "win-" + architecture, "native", "e_sqlcipher.dll"),
                Path.Combine("..", "..", "..", "runtimes", "win-" + architecture, "native", "e_sqlcipher.dll"),
            };

            foreach (var root in ProbeRoots())
            {
                foreach (var tail in relative)
                {
                    string candidate;
                    try
                    {
                        candidate = Path.GetFullPath(Path.Combine(root, tail));
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (probed.Contains(candidate))
                    {
                        continue;
                    }

                    probed.Add(candidate);
                    if (File.Exists(candidate) && LoadLibrary(candidate) != IntPtr.Zero)
                    {
                        return;
                    }
                }
            }
        }

        private static IEnumerable<string> ProbeRoots()
        {
            var roots = new List<string>();

            try
            {
                var location = typeof(SQLiteNative).Assembly.Location;
                if (!string.IsNullOrEmpty(location))
                {
                    roots.Add(Path.GetDirectoryName(location));
                }
            }
            catch (Exception)
            {
                // A dynamic or single file assembly has no location. The base directory below still applies.
            }

            try
            {
                roots.Add(AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception)
            {
            }

            foreach (var root in roots)
            {
                if (!string.IsNullOrEmpty(root))
                {
                    yield return root;
                }
            }
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string path);

        /// <summary>Version string of the loaded SQLite engine, for example <c>3.45.1</c>.</summary>
        public static string EngineVersion
        {
            get
            {
                if (_version == null)
                {
                    EnsureInitialized();
                    using (var connection = new SqliteConnection("Data Source=:memory:"))
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "select sqlite_version();";
                            _version = Convert.ToString(command.ExecuteScalar());
                        }
                    }
                }

                return _version;
            }
        }

        /// <summary>
        /// Version of the SQLCipher layer, for example <c>4.5.2 community</c>, or null when the loaded
        /// native engine is a plain SQLite build without encryption support.
        /// </summary>
        public static string CipherVersion
        {
            get
            {
                if (_cipherVersion == null)
                {
                    EnsureInitialized();

                    try
                    {
                        using (var connection = new SqliteConnection("Data Source=:memory:"))
                        {
                            connection.Open();
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = "pragma cipher_version;";
                                var result = command.ExecuteScalar();
                                _cipherVersion = result == null || result == DBNull.Value
                                    ? string.Empty
                                    : Convert.ToString(result);
                            }
                        }
                    }
                    catch (SqliteException)
                    {
                        _cipherVersion = string.Empty;
                    }
                }

                return string.IsNullOrEmpty(_cipherVersion) ? null : _cipherVersion;
            }
        }

        /// <summary>True when the loaded engine can open and create encrypted databases.</summary>
        public static bool IsEncryptionSupported
        {
            get { return CipherVersion != null; }
        }

        /// <summary>
        /// Asks the engine to abort the statement that is currently running on <paramref name="connection"/>.
        /// Used to honour activity timeouts and workflow cancellation, because
        /// <see cref="SqliteCommand.Cancel"/> is a no-op in Microsoft.Data.Sqlite.
        /// </summary>
        internal static void Interrupt(SqliteConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            try
            {
                var handle = connection.Handle;
                if (handle != null)
                {
                    SQLitePCL.raw.sqlite3_interrupt(handle);
                }
            }
            catch
            {
                // Interrupting is best effort: never let it surface on top of the real error.
            }
        }
    }
}
