# Shaker.SQLLiteDB.Activities

A UiPath activity library for SQLite that needs **no ODBC driver, no System.Data.SQLite install and no
machine level configuration**. The SQLite engine itself ships inside the package (SQLitePCLRaw's
**SQLCipher** build), so publishing the library to Orchestrator is all a robot needs — and **AES-256
encrypted databases work out of the box**.

It is built around the two things that make SQLite awkward in RPA:

* **Many readers at once** — databases are opened in WAL mode, so readers never block each other and
  never block the writer. `SQLite Parallel Query` runs several statements concurrently, each on its own
  read only connection.
* **One writer at a time** — SQLite allows exactly one writer. Instead of letting robots collide and fail
  with *"database is locked"*, every write takes a **lock file** next to the database. Writers queue up
  in an orderly way, across processes and across machines when the database sits on a file share.

On top of that: transactions with savepoints, insert of a whole DataTable with upsert, batch execution,
database encryption, and exports to **CSV, XLSX and JSON** (the .xlsx writer is built into this package,
so Excel does not have to be installed and no Open XML library is dragged into your project).

Every activity has its own icon and draws the fields you actually fill in on the activity itself, the way
the UiPath Database activities do.

---

## Contents

- [Requirements](#requirements)
- [Install](#install)
- [The activities](#the-activities)
- [Quick start](#quick-start)
- [Activity reference](#activity-reference)
- [How concurrency works](#how-concurrency-works)
- [Encryption](#encryption)
- [Exporting data](#exporting-data)
- [Errors, timeouts and cancellation](#errors-timeouts-and-cancellation)
- [Performance notes](#performance-notes)
- [Troubleshooting](#troubleshooting)
- [Building from source](#building-from-source)
- [License](#license)

---

## Requirements

| | |
| --- | --- |
| **Studio** | 2022.10 or newer |
| **Project types** | Windows, and Windows - Legacy |
| **Robot** | Nothing to install. No ODBC driver, no System.Data.SQLite, no registry entry, no DSN. |
| **Package feeds** | The **UiPath Official** feed, enabled by default, for one design time dependency (see below). |
| **Platform** | Windows x64 and x86. The native engine for both ships in the package. |

The package targets three frameworks, which is what lets one package serve both project types:

| Folder | Used by |
| --- | --- |
| `lib/net461` | Windows - Legacy projects |
| `lib/net6.0-windows7.0` | Windows projects — this is the one Studio normally resolves |
| `lib/net6.0` | A fallback for a host that resolves without the Windows flavour |

The SQLite stack and the native engine are packed inside those folders and as
`runtimes/win-x64/native`, so installing the package never asks Studio to resolve them from a feed.

One dependency **is** declared: `System.Activities.ViewModels`. It cannot be bundled, because the
workflow runtime reads every attribute on an activity through `TypeDescriptor` before running it, so the
assembly has to resolve on the robot too and not only in Studio. The UiPath Database activities declare
the same dependency for the same reason, and it restores from the UiPath Official feed.

---

## Install

1. Download `Shaker.SQLLiteDB.Activities.<version>.nupkg` from the
   [Releases page](https://github.com/Mo3bdlaa/Shaker.SQLLiteDB.Activities/releases),
   or build it yourself (see [Building from source](#building-from-source)).
2. Put it in a feed Studio can see: a local folder feed, or your Orchestrator / MyGet / Azure Artifacts
   feed.
3. In Studio: **Manage Packages → Settings**, add the folder as a source, then install
   **Shaker.SQLLiteDB.Activities**.

If you are replacing a version you already installed, delete
`%UserProfile%\.nuget\packages\shaker.sqllitedb.activities\<version>` first. NuGet and Studio cache by
package id *and* version, so reusing a version number otherwise gets you the copy you already had.

### Finding them in the Activities panel

The eight activities a normal automation uses sit at the top level, under **SQLite**. Everything else is
one folder deeper:

| Where | Activities |
| --- | --- |
| **SQLite** | Connect, Disconnect, Execute Query, Execute Scalar, Execute Non Query, Insert Data Table, Transaction Scope, Write Lock Scope |
| **SQLite > Export** | Export To CSV, Export To Excel, Export To JSON, Import CSV |
| **SQLite > Schema** | Table Exists, Get Table Names, Get Table Schema, Create Table |
| **SQLite > Maintenance** | Backup Database, Set Password, Maintenance, Attach Database |
| **SQLite > Advanced** | Connect Scope, Execute Script, Execute Statements, Parallel Query |

The quickest way to find any of them is to type **`SQLite`** in the panel's search box.

If the panel looks empty after installing: custom activity packages are "classic" activities, and with
the Modern design experience on, the panel hides them until you switch that on — Activities panel → the
filter (funnel) icon → **Show Classic**. Then reopen the project; Studio caches the activity list per
project.

### What each activity looks like

Each one carries its own icon, colour coded by what it does, and draws its main inputs and outputs on the
activity itself. Everything else — open mode, journal mode, locking, timeouts, encoding, conflict policy
— stays one click away in the **Properties** panel.

| Colour | What it does |
| --- | --- |
| Blue | Connections and scopes |
| Green | Reads |
| Orange | Writes |
| Purple | Exports |
| Slate | Administration |

The icons and the canvas layout come from `Shaker.SQLLiteDB.Activities.Design.dll`, a WPF assembly that
ships in the `net461` and `net6.0-windows7.0` folders. Studio loads it at design time; a robot never
does, and the runtime assembly references nothing from WPF.

---

## The activities

All 24, and what each one is for. Full property tables are in
**[docs/activities.md](docs/activities.md)**.

### Connecting

| Activity | What it does |
| --- | --- |
| **SQLite Connect** | Opens the database and hands you a `SQLiteConnectionHandle` in its **Connection** output. Pass that to every other activity, then close it with **SQLite Disconnect**. The simplest way to work — no scope to nest things inside. |
| **SQLite Disconnect** | Closes a connection opened by **SQLite Connect**. Without it the connection lives until the job's process ends. |
| **SQLite Transaction Scope** | Commits everything inside as one unit of work, rolls back when an activity throws. Nested scopes use a `SAVEPOINT`, so an inner scope can fail without discarding the outer work. Takes the writer lock for the whole transaction by default. |
| **SQLite Connect Scope** | The same as Connect, except the connection is scoped to a body: activities inside pick it up with nothing to wire, and it is closed however the scope exits. You do not need it — Connect plus Disconnect does the same job — but there is no path that leaves the connection open, and it exposes more tuning (synchronous mode, cache size, retries). |

All three scopes start with a **Sequence** inside them, so you can drop as many activities in as you like.

### Reading

| Activity | Result |
| --- | --- |
| **SQLite Execute Query** | A `DataTable` (+ `RowCount`). Column types are derived from the values that actually came back, or forced to text with `ColumnTyping`. `MaxRows` caps the result. |
| **SQLite Execute Scalar** | One single value instead of a table — `select count(*)`, `select max(id)`. The first value of the first row, plus ready made `TextResult`, `NumberResult` and `IsNull` outputs so you do not have to cast. |
| **SQLite Parallel Query** | `Dictionary(Of String, DataTable)` — several queries at once, each on its own read only connection. |

### Writing

| Activity | Result |
| --- | --- |
| **SQLite Execute Non Query** | **One** INSERT / UPDATE / DELETE / DDL statement. Affected rows (+ `LastInsertRowId`). |
| **SQLite Insert Data Table** | Writes a whole `DataTable` into a table — one prepared statement, batched transactions. Conflict policy: `Abort`, `Ignore`, `Replace`, `Rollback` or `Upsert` (with `KeyColumns` / `UpdateColumns`). Can create the table from the DataTable's shape. This is the activity for "I have rows, put them in the database". |
| **SQLite Execute Statements** | Runs a list of *different* statements in one transaction, with the writer lock taken once — an insert, then an update, then a delete. Not the same as Insert Data Table, which repeats one statement over many rows. Optionally collects failures instead of stopping at the first. |
| **SQLite Execute Script** | A whole SQL *script* — many statements separated by `;`, from a string or a `.sql` file, in one transaction. Use it for a schema file or a migration. |

### Exporting and importing

| Activity | Result |
| --- | --- |
| **SQLite Export To CSV** | Streams a query, a table or a DataTable into a CSV file. Delimiter, quoting, encoding, date format and append are configurable. |
| **SQLite Export To Excel** | A real `.xlsx` workbook: bold frozen header, auto filter, column widths, typed number and date cells. Several queries can go into one workbook, one sheet each. Results beyond the Excel row limit spill into extra sheets. |
| **SQLite Export To JSON** | A JSON array of objects, to a file or straight into a `String` variable. Numbers, booleans and nulls keep their JSON types. |
| **SQLite Import CSV** | Reads a CSV file and loads it, with the same conflict handling as Insert Data Table. |

### Schema

| Activity | Result |
| --- | --- |
| **SQLite Table Exists** | `Boolean`. |
| **SQLite Get Table Names** | `List(Of String)`, optionally including views. |
| **SQLite Get Table Schema** | A `DataTable` with ordinal, column name, declared type, not null, default value and primary key flag. |
| **SQLite Create Table** | Creates a table from the shape of a DataTable, mapping .NET types onto SQLite storage classes. |

### Maintenance

| Activity | What it does |
| --- | --- |
| **SQLite Maintenance** | Housekeeping, one operation per run. `Vacuum` reclaims the space left by deleted rows and shrinks the file. `Analyze` / `Optimize` refresh the statistics the query planner uses. `WalCheckpoint` folds the `-wal` side file back into the database. `IntegrityCheck` and `ForeignKeyCheck` verify the file is not corrupt, and report `IsHealthy`. `Reindex` rebuilds the indexes. |
| **SQLite Backup Database** | A consistent copy through SQLite's online backup API. You *can* copy the file instead — but only when nothing is writing. Copy a live database and you can get a torn file: a half written page, or the `.db` without its matching `-wal`, which restores as data loss. This copies page by page while other robots keep working, and the copy can be given its own password, or none. |
| **SQLite Set Password** | Encrypts a database with AES-256, changes its password, or removes the encryption. |
| **SQLite Attach Database** | Makes a *second* `.db` file visible on the same connection under an alias, so one query can join across both: `select … from main.orders join archive.orders on …`. |

### Locking

| Activity | What it does |
| --- | --- |
| **SQLite Write Lock Scope** | Takes the cross process writer lock once and holds it for everything inside, so another robot cannot slip a write in between yours. The lock is released when the scope exits — on success, on error and on cancellation alike. If another robot already holds it, this one waits, up to `LockTimeoutMilliseconds` (60 s by default), then fails with a clear timeout error rather than hanging. Reports how long it waited. |

---

## Quick start

### Read some rows

```
SQLite Connect            DatabasePath: "C:\Data\orders.db"
                          Connection:   dbConn
SQLite Execute Query      Connection: dbConn
                          Sql: "select id, customer, total from orders where total > @min"
                          Parameters: new Dictionary(Of String, Object) From {{"min", 100}}
                          Data table: dtOrders
SQLite Disconnect         Connection: dbConn
```

No connection string, no driver, no DSN. Connect creates the file when it is missing and turns on WAL.

### Write safely while other robots are running

```
SQLite Connect Scope              DatabasePath: "\\fileserver\share\orders.db"
└── SQLite Write Lock Scope       (takes the lock once for everything inside)
    └── SQLite Transaction Scope  (all or nothing)
        ├── SQLite Execute Non Query   "insert into orders (customer, total) values (@c, @t)"
        └── SQLite Insert Data Table   TableName: "order_lines", DataTable: dtLines
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

### Work with an encrypted database

```
SQLite Connect    DatabasePath: "C:\Data\orders.db"
                  Password:     in_DbPassword      ' from an Orchestrator credential asset
                  Connection:   dbConn
```

Everything else behaves exactly as it does for a plain database.

---

## Activity reference

Every activity and every property, generated from the assembly itself:
**[docs/activities.md](docs/activities.md)**.

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

* Prefer **SQLite Insert Data Table** over a loop of Execute Non Query: one prepared statement and one
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
The message lists every path that was searched. The engine ships in the package four times over — beside
the assemblies, in `x64/` and `x86/` sub folders, and as `runtimes/win-x64/native` — and is loaded by
full path before anything asks for it, so this should not happen. If it does, one of those paths will
tell you where the package landed; the usual cause is a partially restored package, which reinstalling
fixes.

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

## Errors, timeouts and cancellation

Every activity that talks to the database shares three properties:

| Property | Meaning |
| --- | --- |
| `TimeoutMS` | Maximum run time in milliseconds, 120 000 by default. `0` means no limit. When it expires the statement running in the engine is interrupted, not just abandoned. |
| `ContinueOnError` | When `True` the workflow carries on even if the activity throws. |
| `ErrorMessage` | The message of the error that was swallowed because `ContinueOnError` was set. Empty when the activity succeeded. |

Stopping a job cancels the statement in flight the same way a timeout does, so a long `VACUUM` or a big
export does not keep a robot busy after you have asked it to stop.

Errors arrive as `SQLiteActivityException`, or `SQLiteLockTimeoutException` when the writer lock could
not be taken in time. Both carry a message that names the file, and for a lock timeout, the machine, user
and process that is holding it.

---

## Building from source

```bash
dotnet build   Shaker.SQLLiteDB.Activities.sln -c Release
dotnet test    tests/Shaker.SQLLiteDB.Activities.Tests/Shaker.SQLLiteDB.Activities.Tests.csproj
dotnet pack    src/Shaker.SQLLiteDB.Activities/Shaker.SQLLiteDB.Activities.csproj -c Release
# the .nupkg lands in ./artifacts
```

Or use the helper scripts: `./build.sh` (Linux/macOS) and `.\build.ps1` (Windows).

`NuGet.config` adds the UiPath Official feed, which is where the build gets the `6.0.0.0` build of
`System.Activities` that Studio ships, plus the designer and view model assemblies. The .NET SDK 8 builds
all three target frameworks on any operating system — `net461` through the reference assemblies package,
and the WPF designers with `EnableWindowsTargeting` — so CI needs no Windows machine, though the release
workflow uses one anyway.

To publish a release, run the `release` workflow with a version tag, or push the tag:

```bash
git tag v1.0.0 && git push origin v1.0.0
```

It builds, tests, packs, checks the package layout and attaches the package to a GitHub Release.

### Layout

```
src/Shaker.SQLLiteDB.Activities
├── Core/          connection settings, connection and transaction handles, writer lock,
│                  retry policy, command execution, bulk writer, schema helpers
├── IO/            CSV reader and writer, XLSX writer, JSON writer
├── Activities/    the UiPath activities (a NativeActivity shell that reads the enclosing
│                  scope plus an async worker that does the database work off the workflow thread)
└── Design/        view models, for the Studio Web designer

src/Shaker.SQLLiteDB.Activities.Design
└── Designers/     one WPF ActivityDesigner per activity: the panel icon and the canvas layout

tools/             the generators that keep the designers, the view models and docs/activities.md
                   in step with the activities themselves
tests/             unit and workflow level tests covering the engine, the writer lock, bulk
                   writes, the file formats, and the activities running in the real workflow runtime
docs/activities.md the generated property reference
```

Three things are generated rather than hand written, because each has to list every property of every
activity and would rot immediately otherwise:

```bash
# after building, dump the activity metadata with tools/dump-activity-metadata.cs, then:
python3 tools/generate-designers.py           # src/Shaker.SQLLiteDB.Activities.Design/Designers
python3 tools/generate-viewmodels.py          # src/Shaker.SQLLiteDB.Activities/Design
python3 tools/generate-activity-reference.py  # docs/activities.md
```

---

## License

MIT — see [LICENSE](LICENSE).

SQLCipher Community Edition is © Zetetic LLC, BSD licensed, and is redistributed through the
`SQLitePCLRaw.bundle_e_sqlcipher` package.
