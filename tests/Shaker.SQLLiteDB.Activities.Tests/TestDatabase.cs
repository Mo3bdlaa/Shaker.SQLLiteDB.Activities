using System;
using System.IO;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Tests
{
    /// <summary>A throwaway database in its own folder, removed when the test ends.</summary>
    public sealed class TestDatabase : IDisposable
    {
        public TestDatabase(string name = "test.db")
        {
            Folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "shaker-sqlite-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Folder);
            Path = System.IO.Path.Combine(Folder, name);
        }

        public string Folder { get; }

        public string Path { get; }

        public SQLiteConnectionSettings Settings()
        {
            return new SQLiteConnectionSettings { DatabasePath = Path };
        }

        public SQLiteConnectionHandle Open()
        {
            return SQLiteConnectionHandle.Open(Settings());
        }

        public string File(string name)
        {
            return System.IO.Path.Combine(Folder, name);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Folder))
                {
                    Directory.Delete(Folder, true);
                }
            }
            catch (IOException)
            {
                // A leftover temp folder is not worth failing a test over.
            }
        }
    }
}
