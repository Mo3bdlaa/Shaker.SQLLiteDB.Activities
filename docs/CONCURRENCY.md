# Concurrency guide

SQLite is a file, not a server. Everything about running it from several robots comes down to two facts:

1. **Readers are cheap and unlimited** — in WAL mode.
2. **There is exactly one writer at a time** — always.

This library is built to make both of those comfortable from a workflow. This document explains what it
does, so you can pick the right setup for your process.

---

## What the library sets up for you

When a **SQLite Connect Scope** opens a database it applies:

| PRAGMA | Value | Why |
| --- | --- | --- |
| `journal_mode` | `WAL` | Readers and the writer stop blocking each other. The setting is stored in the file, so it only has to be applied once, but applying it again is harmless. |
| `synchronous` | `NORMAL` | The safe and fast combination with WAL. |
| `busy_timeout` | 30 000 ms | The engine waits instead of failing immediately when another connection holds a lock. |
| `foreign_keys` | `ON` | SQLite has constraints off by default, which surprises everyone once. |

WAL creates two side car files next to the database, `-wal` and `-shm`. They are normal and are cleaned
up when the last connection closes. Run **SQLite Maintenance → WalCheckpoint** if the `-wal` file grows
large after a heavy load.

---

## Reading in parallel

Anything that only reads can run at the same time as anything else:

* several workflows in one robot,
* several robots on one machine,
* several machines pointing at the same file.

Read activities never take the writer lock. If you want several queries to run at the same time inside
one workflow, use **SQLite Parallel Query** — it opens one read only connection per query:

```
SQLite Parallel Query
    DatabasePath: "C:\Data\orders.db"
    Queries: new Dictionary(Of String, String) From {
        {"orders",    "select * from orders where created >= date('now','-7 day')"},
        {"customers", "select * from customers"},
        {"totals",    "select customer_id, sum(total) as total from orders group by customer_id"}
    }
    MaxDegreeOfParallelism: 4
    Result: dictResults      ' dictResults("orders") is a DataTable
```

A `Parallel` activity with a **SQLite Execute Query** in each branch works too: the activities hand the
workflow thread back while the database work runs, so the branches really do overlap.

---

## Writing without collisions

### The problem

Two robots writing at the same moment: one succeeds, the other gets `SQLITE_BUSY` — *"database is
locked"*. The `busy_timeout` helps, but under real contention it only moves the failure around, and a
long write can still starve a short one.

### What this library does

Before any write, the activity takes a **writer lock**:

```
<database>.writelock          opened exclusively (FileShare.None) for as long as the write lasts
<database>.writelock.owner    machine, user, process id and start time of the current holder
```

* Another robot, in another process or on another machine, cannot open the lock file while it is held, so
  it waits — in an orderly queue instead of a race.
* If a robot crashes, the operating system closes its handles, and the lock is free again. There is no
  stale lock to clean up by hand.
* If the lock cannot be taken within `LockTimeoutMilliseconds`, the activity fails with a message that
  names the current holder, read from the `.owner` file.
* Should the engine still report a busy or locked error — for example because a tool that does not use
  this library is writing — the statement is retried with an exponential backoff
  (`RetryAttempts`, `RetryInitialDelayMilliseconds` on the Connect Scope).

### Taking the lock once for a group of writes

Per activity locking is right for a single statement. It is wrong when several writes belong together,
because another robot can slip in between them. Wrap them instead:

```
SQLite Write Lock Scope            ' lock taken once here
├── SQLite Execute Non Query       ' notices the scope, does not lock again
├── SQLite Bulk Insert             ' same
└── SQLite Execute Non Query
```

Add atomicity with a transaction:

```
SQLite Transaction Scope           ' BEGIN IMMEDIATE + writer lock
├── ...
└── ...                            ' COMMIT at the end, ROLLBACK if anything throws
```

Nested **SQLite Transaction Scopes** use `SAVEPOINT`, so an inner scope can roll back its own work
without discarding the outer transaction.

One thing to know about the scopes: they take the lock while the workflow thread is theirs, so a scope
that has to wait for a busy database holds up the other branches of the same workflow until it gets in.
The individual activities do not behave that way — their database work runs off the workflow thread, so
parallel branches keep moving. In practice this only shows up under heavy contention; if a workflow both
reads in parallel and writes under contention, do the reads first and then enter the write scope.

---

## Deployment patterns

### One robot, one machine

Nothing to configure. Defaults are fine. You can even set `LockScope` to `Process`, which skips the lock
file, if you are certain nothing else touches the database.

### Several processes on one machine

Defaults are fine: the lock file is on the same disk as the database and all processes see it.

### Several machines, database on a file share

Works, and the lock file is what makes it work — but keep in mind:

* Put `LockFilePath` on the **same share** as the database (the default does this) so that every machine
  locks the same file.
* Keep write transactions short. A robot holding the lock for a minute blocks everyone else for a minute.
* Expect the file share, not SQLite, to be your bottleneck. If the process is write heavy, a client /
  server database is a better fit than SQLite.
* Never point two robots at the same database with different `LockFilePath` values — they will not see
  each other.

### A read replica

For reporting on a busy database, take a snapshot with **SQLite Backup Database** and read from the copy.
The backup uses the SQLite online backup API, so it is consistent even while writes are happening —
unlike copying the file with a File activity. Encrypted databases are copied with SQLCipher's export
instead, which reads a consistent WAL snapshot.

### Encrypted databases

Encryption changes nothing about the concurrency model: an encrypted database still uses WAL, still
allows many readers, and still takes the same writer lock. The one activity that behaves differently is
**SQLite Set Password**, which replaces the database file when it encrypts or decrypts: it takes the
writer lock, and the database must not be open anywhere else at that moment.

---

## Rules of thumb

* Wrap writes in a **Transaction Scope** or a **Write Lock Scope** when they belong together.
* Use **SQLite Bulk Insert** instead of a loop of inserts. It is both faster and shorter in the lock.
* Do not hold a transaction open across a slow activity (a web call, a UI interaction). The writer lock
  is held for that whole time.
* Leave `LockScope` on `Machine` unless you have a specific reason not to.
* Read only workflows do not need any of this; just read.
