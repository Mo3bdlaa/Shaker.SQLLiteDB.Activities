# Recipes

Copy-paste patterns for common jobs. Expressions are written in VB, the default in UiPath Studio.

---

## Create the database and its schema on first run

```
SQLite Connect Scope            DatabasePath: in_DbPath
└── SQLite Execute Script       Script:
    "create table if not exists customers (
       id      integer primary key,
       name    text not null,
       email   text unique,
       created text not null default (datetime('now'))
     );
     create table if not exists orders (
       id          integer primary key autoincrement,
       customer_id integer not null references customers(id),
       total       real    not null,
       created     text    not null default (datetime('now'))
     );
     create index if not exists ix_orders_customer on orders (customer_id);"
```

The file and its folder are created automatically, and the script runs in one transaction.

---

## Insert one row and keep its id

```
SQLite Execute Non Query
    Sql: "insert into customers (name, email) values (@name, @email)"
    Parameters: new Dictionary(Of String, Object) From {
        {"name",  row("Name").ToString},
        {"email", row("Email").ToString}
    }
    Result: affectedRows
    LastInsertRowId: newCustomerId
```

Always use parameters. They are faster, and they make injection impossible.

---

## Load a DataTable, updating what is already there

```
SQLite Bulk Insert
    TableName: "customers"
    DataTable: dtFromExcel
    ConflictPolicy: Upsert
    KeyColumns: {"id"}
    BatchSize: 1000
    Result: affectedRows
```

`Upsert` needs the key columns to be covered by a primary key or a unique index.
Use `Ignore` to skip duplicates instead, or `Replace` to overwrite them.

---

## Apply a set of changes atomically

```
SQLite Connect Scope
└── SQLite Transaction Scope
    ├── SQLite Execute Non Query  "update accounts set balance = balance - @amount where id = @from"
    ├── SQLite Execute Non Query  "update accounts set balance = balance + @amount where id = @to"
    └── SQLite Execute Non Query  "insert into transfers (from_id, to_id, amount) values (@from, @to, @amount)"
```

If any of them throws, nothing is written and the writer lock is released.

---

## Build a multi sheet report

```
SQLite Export To Excel
    FilePath: "C:\Reports\" & Now.ToString("yyyy-MM-dd") & ".xlsx"
    Sheets: new Dictionary(Of String, String) From {
        {"Orders",    "select * from orders where created >= date('now','-30 day')"},
        {"Customers", "select * from customers order by name"},
        {"Totals",    "select c.name, sum(o.total) as total
                       from orders o join customers c on c.id = o.customer_id
                       group by c.name order by total desc"}
    }
    Result: reportPath
```

---

## Export a big table without running out of memory

```
SQLite Export To CSV
    SourceTableName: "events"
    FilePath: "C:\Out\events.csv"
    Delimiter: ";"
    Encoding: "utf-8"
    Result: csvPath
    RowsExported: exportedRows
```

Rows are streamed from the database straight into the file.

---

## Feed an API with JSON

```
SQLite Export To JSON
    Sql: "select id, name, email from customers where created >= @since"
    Parameters: new Dictionary(Of String, Object) From {{"since", DateTime.Today.AddDays(-1)}}
    FilePath: ""            ' empty: give the JSON back as text
    Result: jsonPayload
```

---

## Import a CSV that arrives every morning

```
SQLite Connect Scope
└── SQLite Write Lock Scope
    ├── SQLite Import CSV       FilePath: in_File
    │                           TableName: "staging_customers"
    │                           CreateTableIfNotExists: True
    │                           ConflictPolicy: Replace
    └── SQLite Execute Non Query
            "insert into customers (id, name, email)
             select id, name, email from staging_customers
             where true
             on conflict(id) do update set name = excluded.name, email = excluded.email"
```

---

## Encrypt a database, then use it

```
SQLite Set Password         DatabasePath: "C:\Data\orders.db"
                            NewPassword:  in_Credential.Password    ' from an Orchestrator asset
                            Result:       out_message

SQLite Connect Scope        DatabasePath: "C:\Data\orders.db"
                            Password:     in_Credential.Password
└── SQLite Execute Query    "select * from orders"
```

Run **SQLite Set Password** outside the Connect Scope: encrypting rewrites the database file.
To change the password later, put the current one in `Password` and the new one in `NewPassword`;
to remove the encryption, leave `NewPassword` empty.

---

## Hand an encrypted extract to someone else

```
SQLite Backup Database   DatabasePath:    "C:\Data\orders.db"
                         Password:        in_DbPassword
                         DestinationPath: "D:\Share\orders-extract.db"
                         BackupPassword:  in_SharePassword     ' the copy gets its own password
```

---

## Several robots writing to one database on a share

```
SQLite Connect Scope
    DatabasePath: "\\fileserver\rpa\shared.db"
    LockScope: Machine                 ' the default
    LockTimeoutMilliseconds: 120000    ' patient: other robots may be mid-write
    BusyTimeoutMilliseconds: 30000
└── SQLite Write Lock Scope
    └── SQLite Bulk Insert  TableName: "results", DataTable: dtResults
```

Every robot uses the same `\\fileserver\rpa\shared.db.writelock` file, so they queue instead of failing.

---

## Nightly housekeeping

```
SQLite Connect Scope
├── SQLite Maintenance   Operation: WalCheckpoint
├── SQLite Maintenance   Operation: Analyze
├── SQLite Maintenance   Operation: IntegrityCheck    ' IsHealthy -> alert when False
└── SQLite Backup Database  DestinationPath: "D:\Backups\orders-" & Now.ToString("yyyyMMdd") & ".db"
```

`Vacuum` cannot run inside a transaction scope; put it directly in the connect scope.

---

## Carry on when a statement fails

```
SQLite Execute Non Query
    Sql: "insert into audit (message) values (@m)"
    ContinueOnError: True
    ErrorMessage: out_auditError     ' empty when it worked
```
