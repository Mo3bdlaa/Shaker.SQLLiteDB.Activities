# Shaker.SQLLiteDB.Activities

A UiPath activity library for SQLite that needs **no ODBC driver, no System.Data.SQLite install and no
machine level configuration**. The SQLite engine itself ships inside the package (SQLitePCLRaw's
**SQLCipher** build), so publishing the library to Orchestrator is all a robot needs — and **AES-256
encrypted databases work out of the box**.

It is built around the two things that make SQLite awkward in RPA:

* **Many readers at once** — the library opens databases in WAL mode, so readers never block each other
  and never block the writer. `SQLite Parallel Query` runs several statements concurrently, each on its
  own read only connection.
* **One writer at a time** — SQLite allows exactly one writer. Instead of letting robots collide and fail
  with *"database is locked"*, every write takes a **lock file** next to the database. Writers queue up
  in an orderly way, across processes and across machines when the database sits on a file share.

On top of that: transactions with savepoints, bulk insert and upsert, batch execution, database
encryption, and exports to **CSV, XLSX and JSON** (the .xlsx writer is built into this package, so Excel
does not have to be installed and no Open XML library is dragged into your project).

---

## Contents

- [Install](#install)
- [Quick start](#quick-start)
- [Activity reference](#activity-reference)
- [How concurrency works](#how-concurrency-works)
- [Encryption](#encryption)
- [Exporting data](#exporting-data)
- [Performance notes](#performance-notes)
- [Troubleshooting](#troubleshooting)
- [Building from source](#building-from-source)

---

## Install

The package targets both UiPath project types:

| UiPath project | Target framework in the package |
| --- | --- |
| Windows - Legacy | `net461` |
| Windows | `net6.0` |

Studio matches a **Windows** project against the plain `net6.0` folder.

Two things decide whether Studio accepts the package at all, and both are easy to get wrong:

* **The `System.Activities` version the assembly binds to.** Studio ships `6.0.0.0`. `UiPath.Workflow.Runtime`
  on nuget.org is newer and produces a `6.0.3.0` reference, which Studio cannot load — it reports the
  whole package as *"not compatible with Windows projects"*. The build therefore takes
  `UiPath.Workflow.Runtime` from UiPath's own feed (see `NuGet.config`), which is the `6.0.0.0` build,
  and references it with `PrivateAssets="all"` so it never becomes a package dependency.
* **The lib folders.** `net461` and `net6.0`. Newer UiPath packages ship `net6.0-windows7.0`, but that
  is for newer Studio versions.

The package also **declares no dependencies at all**: the SQLite stack and the native engine are packed
inside `lib/`, so installing it never asks Studio to resolve anything from a feed. That is what "no ODBC
driver and nothing to install" is supposed to mean, and it removes a whole class of install failures.

1. Download `Shaker.SQLLiteDB.Activities.<version>.nupkg` from the
   [Releases page](https://github.com/Mo3bdlaa/Shaker.SQLLiteDB.Activities/releases),
   or build it yourself (see [Building from source](#building-from-source)).
2. Put it in a feed Studio can see: a local folder feed, or your Orchestrator / MyGet / Azure Artifacts feed.
3. In Studio: **Manage Packages → Settings**, add the folder as a source, then install
   **Shaker.SQLLiteDB.Activities**.

### Finding them in the Activities panel

The 23 activities live under the **`Shaker.SQLLiteDB.Activities.Activities`** node of the panel, which is
built from the namespace. The quickest way to see them is to type **`SQLite`** in the panel's search box.

If the panel stays empty, work through these in order:

1. **Show classic activities.** Custom activity packages are "classic" activities. With the Modern
   design experience on, the panel hides them until you switch that on: Activities panel → the filter
   (funnel) icon → **Show Classic**. This is the most common reason a freshly installed custom package
   looks like it installed nothing.
2. **Check the assembly actually loaded.** Open the **Imports** panel and look for
   `Shaker.SQLLiteDB.Activities.Activities`. If it is listed, the assembly loaded and the problem is
   only the panel filter above. If it is missing, the package did not load — carry on to step 3.
3. **Check the dependencies restored.** The package needs `Microsoft.Data.Sqlite.Core` and
   `SQLitePCLRaw`, which Studio pulls from nuget.org. If your only package source is a local folder,
   the package installs but its assembly cannot load, and nothing appears. Re-enable nuget.org in
   Manage Packages → Settings and reinstall.
4. **Reopen the project** after installing. Studio caches the activity list per project.

**Leave `nuget.org` enabled** as a package source while installing. The package depends on
`Microsoft.Data.Sqlite.Core` and `SQLitePCLRaw`, and Studio restores those from there. If the only
source is your local folder, the package installs but its assembly cannot load, and the activity panel
stays empty.

---

## Quick start

### Read some rows

```
SQLite Connect Scope           DatabasePath: "C:\Data\orders.db"
└── SQLite Execute Query       Sql: "select id, customer, total from orders where total > @min"
                               Parameters: new Dictionary(Of String, Object) From {{"min", 100}}
                               Result: dtOrders
```

No connection string, no driver, no DSN. The scope opens the file (creating it when missing), turns on
WAL and closes everything again when the sequence ends, also when an activity throws.

### Write safely while other robots are running

```
SQLite Connect Scope              DatabasePath: "\\fileserver\share\orders.db"
└── SQLite Write Lock Scope       (takes the lock once for everything inside)
    └── SQLite Transaction Scope  (all or nothing)
        ├── SQLite Execute Non Query  "insert into orders (customer, total) values (@c, @t)"
        └── SQLite Bulk Insert        TableName: "order_lines", DataTable: dtLines
```

Every other robot that wants to write waits its turn instead of failing. Readers are not affected at all.

### Export a report

```
SQLite Export To Excel   Sql: "select * from orders where created >= @from"
                         Parameters: {"from", DateTime.Today.AddDays(-7)}
                         FilePath: "C:\Reports\weekly.xlsx"
                         SheetName: "Orders"
```

### Load a CSV into a table

```
SQLite Import CSV   FilePath: "C:\In\customers.csv"
                    TableName: "customers"
                    CreateTableIfNotExists: True
                    ConflictPolicy: Upsert
                    KeyColumns: {"id"}
```

---

## Activity reference

### Scopes

| Activity | What it does |
| --- | --- |
| **SQLite Connect Scope** | Opens the database and shares the connection with every SQLite activity inside it. Applies the PRAGMA tuning (WAL, busy timeout, synchronous, foreign keys) and closes the connection on success, on error and on cancellation. Also outputs the connection, if you prefer to pass it around by hand. |
| **SQLite Transaction Scope** | Commits everything inside as one unit of work, rolls back when an activity throws. Nested scopes use a `SAVEPOINT`, so an inner scope can fail without discarding the outer work. Takes the writer lock for the whole transaction by default. |
| **SQLite Write Lock Scope** | Takes the cross process writer lock once and holds it for everything inside, so a group of writes cannot be interleaved with another robot's writes. Reports how long it waited. |

### Reading

| Activity | Result |
| --- | --- |
| **SQLite Execute Query** | `DataTable` (+ `RowCount`). Column types are derived from the values that actually came back, or forced to text with `ColumnTyping`. `MaxRows` caps the result. |
| **SQLite Execute Scalar** | The first value of the first row, plus ready made `TextResult`, `NumberResult` and `IsNull` outputs. |
| **SQLite Parallel Query** | `Dictionary(Of String, DataTable)` — several queries at once, each on its own read only connection. |

### Writing

| Activity | Result |
| --- | --- |
| **SQLite Execute Non Query** | Affected rows (+ `LastInsertRowId`). |
| **SQLite Bulk Insert** | Rows written, using one prepared statement and batched transactions. Conflict policy: `Abort`, `Ignore`, `Replace`, `Rollback` or `Upsert` (with `KeyColumns` / `UpdateColumns`). Can create the table from the DataTable. |
| **SQLite Execute Batch** | Runs a `List(Of SQLiteStatement)` in one transaction, with the lock taken once. Optionally collects failures instead of stopping. |
| **SQLite Execute Script** | Runs a multi statement script from a string or a `.sql` file, in one transaction. |
| **SQLite Import CSV** | Reads a CSV file and bulk loads it, with the same conflict handling as the bulk insert. |

### Exporting

| Activity | Result |
| --- | --- |
| **SQLite Export To CSV** | Streams a query, a table or a DataTable into a CSV file. Delimiter, quoting, encoding, date format and append are configurable. |
| **SQLite Export To Excel** | Writes a real `.xlsx` workbook: bold frozen header, auto filter, column widths, typed number and date cells. Several queries can go into one workbook, one sheet each. Results beyond the Excel row limit spill into extra sheets. |
| **SQLite Export To JSON** | A JSON array of objects, to a file or straight into a `String` variable. Numbers, booleans and nulls keep their JSON types. |

### Schema and housekeeping

| Activity | Result |
| --- | --- |
| **SQLite Table Exists** | `Boolean`. |
| **SQLite Get Table Names** | `List(Of String)`, optionally including views. |
| **SQLite Get Table Schema** | `DataTable` with ordinal, column name, declared type, not null, default value and primary key flag. |
| **SQLite Create Table** | Creates a table from the shape of a DataTable. |
| **SQLite Maintenance** | `VACUUM`, `ANALYZE`, `PRAGMA optimize`, WAL checkpoint, integrity check, foreign key check, `REINDEX`. Reports `IsHealthy` for the checks. |
| **SQLite Set Password** | Encrypts a database, changes its password, or removes the encryption. |
| **SQLite Backup Database** | A consistent copy through the SQLite online backup API — safe while the database is in use, unlike copying the file. Encrypted databases are copied with SQLCipher's export, and the copy can get its own password. |
| **SQLite Attach Database** | Attaches a second database file under an alias so one query can join both. |
| **SQLite Disconnect** | Closes a connection that was opened outside a scope. |

Every activity also has the usual UiPath properties: `TimeoutMS`, `ContinueOnError` (with an
`ErrorMessage` output), and the connection properties that let it run standalone, without a scope.

---

## How concurrency works

### Many readers

The Connect Scope sets `journal_mode = WAL`. In WAL mode a reader sees a consistent snapshot and never
waits for the writer, and the writer never waits for readers. So:

* several workflows, robots or machines can read the same `.db` file at the same time;
* `SQLite Parallel Query` opens one read only connection per query and runs them concurrently;
* read activities never take the writer lock (unless you switch `LockReads` on, which is only useful for
  non-WAL databases on a file share).

### One writer, without the "database is locked" lottery

SQLite itself allows one writer at a time. Left alone, two robots writing at the same moment produce
`SQLITE_BUSY` errors in whichever one loses. This library adds two layers on top:

1. **A lock file.** Before a write, the activity opens `<database>.writelock` exclusively. The next
   writer, in any process on any machine that can reach the file, waits for it. The operating system
   releases the handle when a process dies, so a crashed robot cannot leave a stale lock behind.
   A side car file `<database>.writelock.owner` records machine, user and process id of the current
   holder, which is what a lock timeout message reports.
2. **A retry policy.** If the engine still reports `SQLITE_BUSY` or `SQLITE_LOCKED` — for example because
   a process that does not use this library is writing — the statement is retried with an exponential
   backoff before the activity gives up.

Settings (on every activity, and on the Connect Scope for everything inside it):

| Property | Meaning |
| --- | --- |
| `LockScope` | `Machine` (default, lock file), `Process` (in-memory only), `None` (rely on SQLite alone). |
| `LockFilePath` | Where the lock file lives. Empty means `<database>.writelock`. Point several robots at the same path to serialize them. |
| `LockTimeoutMilliseconds` | How long to wait for the lock before failing. Default 60 s. |
| `BusyTimeoutMilliseconds` | How long SQLite itself waits for an engine level lock. Default 30 s. |
| `RetryAttempts`, `RetryInitialDelayMilliseconds` | The retry policy, on the Connect Scope. |

**Grouping writes.** Taking the lock per activity is fine for single statements. When several writes
belong together, wrap them in a **SQLite Write Lock Scope**: the lock is taken once, held for the whole
group, and the activities inside notice it and do not take it again. A **SQLite Transaction Scope** does
the same and adds all-or-nothing semantics.

**A note on file shares.** Running a SQLite database on a network share is possible with this library,
because the writer lock is a plain file lock, but SQLite's own advice still applies: local disk is
safer and much faster. If you must use a share, keep transactions short and leave `LockScope` on
`Machine`.

---

## Encryption

The engine bundled here is a **SQLCipher** build, so encryption needs nothing extra installed. It is a
superset of plain SQLite: databases without a password behave exactly as they always did.

### Using an encrypted database

Fill in `Password` on the Connect Scope (or on a standalone activity) and everything else works
unchanged — queries, bulk insert, exports, WAL, the writer lock:

```
SQLite Connect Scope    DatabasePath: "C:\Data\orders.db"
                        Password: in_DbPassword
```

Store the password in an Orchestrator **Credential asset**, never in the workflow.

A wrong or missing password fails on the Connect Scope with a message that says so. That check is
deliberate: SQLite itself happily *opens* an encrypted file without the key and only complains at the
first read, which would otherwise surface much later as a confusing "file is not a database".

### Encrypting, re-keying, decrypting

```
SQLite Set Password   DatabasePath: "C:\Data\orders.db"
                      Password:     ""              ' the current one, empty when not yet encrypted
                      NewPassword:  in_NewPassword  ' empty removes the encryption
                      Result:       message
                      BackupPath:   out_backup
```

* **Changing** the password of an already encrypted database happens in place (`PRAGMA rekey`).
* **Encrypting** a plain database, or **decrypting** an encrypted one, cannot be done in place: SQLCipher
  writes a new file and this activity swaps it in, keeping the previous file as `<database>.bak` unless
  you switch `KeepBackup` off. Tables, indexes, views and triggers all come across.
* Because the file is replaced, this activity must run **outside** a Connect Scope; it says so plainly if
  you nest it.

### What it protects, and what it does not

The whole file is encrypted with AES-256, including the header, so nothing readable is left on disk — no
table names, no stray strings. It protects the database **at rest**: a copied or stolen `.db` file is
useless without the password. It does not protect against someone who can read the password your robot
uses, and the pages are decrypted in memory while the workflow runs.

Third party notice: SQLCipher Community Edition is © Zetetic LLC, BSD licensed, and is redistributed
through the `SQLitePCLRaw.bundle_e_sqlcipher` package.

---

## Exporting data

* **CSV** is streamed straight from the data reader, so a million row export uses no more memory than a
  hundred row one. Quoting follows RFC 4180: a field is quoted only when it contains the delimiter, a
  quote or a line break, unless you ask for `QuoteAllFields`.
* **XLSX** is written by this package itself — a SpreadsheetML workbook inside a zip container. That
  keeps the package free of `DocumentFormat.OpenXml` and its habit of colliding with the version UiPath's
  own Excel activities bring along. Numbers stay numbers, dates get a real date format, booleans become
  `TRUE`/`FALSE`, and `NULL` becomes a genuinely empty cell.
* **JSON** keeps real JSON types: numbers unquoted, `true`/`false` for booleans, `null` for `NULL`,
  ISO 8601 for dates, Base64 for blobs.

---

## Performance notes

* Prefer **SQLite Bulk Insert** over a loop of Execute Non Query: one prepared statement and one
  transaction per batch instead of one transaction per row — typically two orders of magnitude faster.
* Keep `BatchSize` around 1000 for large loads. `0` puts everything in one transaction, which is fastest
  but holds the writer lock for the whole load.
* Put read heavy workflows in a Connect Scope and reuse the connection instead of letting each activity
  open its own.
* Run **SQLite Maintenance → WalCheckpoint** after a big load if the `-wal` file has grown large, and
  **Vacuum** occasionally after deleting a lot of data.
* `synchronous = Normal` (the default) together with WAL is both safe and fast. `Off` is faster still but
  can corrupt the database if the machine loses power.

---

## Troubleshooting

**"Timed out after 60000 ms waiting for the writer lock…"**
Another writer is holding the lock. The message names the machine, user and process. Either the other
side runs a long transaction (shorten it), or a workflow crashed while holding the lock file — in that
case the operating system has already released it, so simply retry. Raise `LockTimeoutMilliseconds` if
your writes are legitimately long.

**"database is locked" even though the lock is configured**
Something outside this library is writing to the same file — another tool, or a robot whose
`LockFilePath` points somewhere else. Make sure every writer uses the same lock file path.

**"The embedded SQLite engine (e_sqlcipher) could not be loaded"**
The native files that come with `SQLitePCLRaw.bundle_e_sqlite3` were not deployed next to the assembly.
Re-install the package in the project so that NuGet restores its dependencies; in Windows - Legacy
projects check that the `runtimes\win-x64\native` folder made it into the published package.

**"The database could not be read with the password that was supplied"**
Either the password is wrong, or the file is not encrypted at all (supplying a password for a plain
database fails the same way — leave `Password` empty for those).

**"The loaded SQLite engine has no encryption support"**
Another package in the project has pulled in a plain `e_sqlite3` native engine, which wins over the
SQLCipher one at load time. Check the project's dependencies for another SQLite package.

**Values come back as `Object`**
A SQLite column is dynamically typed, so a column that contains both numbers and text cannot have one
.NET type. Set `ColumnTyping` to `AllText` if you want strings everywhere.

---

## Building from source

```bash
dotnet build   Shaker.SQLLiteDB.Activities.sln -c Release
dotnet test    tests/Shaker.SQLLiteDB.Activities.Tests/Shaker.SQLLiteDB.Activities.Tests.csproj
dotnet pack    src/Shaker.SQLLiteDB.Activities/Shaker.SQLLiteDB.Activities.csproj -c Release
# the .nupkg lands in ./artifacts
```

Or use the helper scripts: `./build.sh` (Linux/macOS) and `.\build.ps1` (Windows).

To publish a release, push a version tag. The `release` workflow builds, tests, packs and attaches the
package to a GitHub Release:

```bash
git tag v1.0.1 && git push origin v1.0.1
```

The .NET SDK 8 builds both target frameworks on any operating system; the `net461` output is produced
with the reference assemblies package, so no Windows machine is required for CI.

### Layout

```
src/Shaker.SQLLiteDB.Activities
├── Core/          connection settings, connection and transaction handles, writer lock,
│                  retry policy, command execution, bulk writer, schema helpers
├── IO/            CSV reader and writer, XLSX writer, JSON writer
└── Activities/    the UiPath activities (a NativeActivity shell that reads the enclosing
                   scope plus an async worker that does the database work off the workflow thread)
tests/             unit and workflow level tests covering the engine, the writer lock, bulk
                   writes, the file formats, and the activities running in the real workflow runtime
```

## License

MIT — see [LICENSE](LICENSE).
