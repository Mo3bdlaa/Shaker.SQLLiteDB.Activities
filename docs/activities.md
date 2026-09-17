# Activity reference

Every activity and every property it has. Generated from the assembly itself, so it cannot
drift from what Studio shows you.

The **In/Out** column says whether a property is an argument you can bind to a variable or
expression (In / Out) or a plain value you pick in the Properties panel (Value). Properties
marked **required** must be set for the activity to validate.

---

## SQLite

The everyday ones, at the top level of the panel.

### SQLite Connect

Opens a SQLite database and returns the connection. No ODBC driver or SQLite installation is required.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Database path | `string` | In | Input | Full path of the SQLite database file, for example C:\Data\orders.db. The file and its folder are created when they do not exist. |
| Password | `string` | In | Input | Password of an encrypted database. Leave empty for a normal database. |
| Connection string | `string` | In | Options | Complete connection string, for full control. Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Options | ReadWriteCreate creates the database when missing, ReadOnly never takes the writer lock, Memory keeps everything in RAM. |
| Journal mode | `SQLiteJournalMode` | Value | Options | WAL lets many readers work while one writer is active. Recommended. |
| Enforce foreign keys | `bool` | Value | Options | Turn foreign key constraints on. SQLite has them off by default. |
| Busy timeout (ms) | `int` | In | Options | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. |
| Lock timeout (ms) | `int` | In | Locking | How long a write activity waits for the writer lock before it fails. |
| Connection | `SQLiteConnectionHandle` | Out | Output | The open connection. Pass it to the other SQLite activities, and close it with SQLite Disconnect. |
| SQLite version | `string` | Out | Output | Version of the embedded SQLite engine, for example 3.45.1. |

### SQLite Disconnect

Closes a SQLite connection that was opened outside a scope.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Connection *(required)* | `SQLiteConnectionHandle` | In | Input | The connection to close. Closing an already closed connection does nothing. |
| Closed | `bool` | Out | Output | True when the connection was open and has now been closed. |

### SQLite Execute Query

Runs a SELECT statement and returns a DataTable. Safe to run in parallel with other readers and with a writer.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| SQL *(required)* | `string` | In | Input | The SQL statement. Use named parameters (@name) or question marks, never string concatenation. |
| Parameters | `Dictionary(Of String, Object)` | In | Input | Named parameters as a Dictionary(Of String, Object). The leading @ is optional. Keeps the query safe from SQL injection. |
| Parameter values | `IEnumerable(Of Object)` | In | Input | Values for the '?' placeholders of the statement, in order. An alternative to named parameters. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Column typing | `SQLiteColumnTyping` | Value | Options | Auto derives each column type from the values returned, Declared trusts the provider, AllText returns every column as text. |
| Max rows | `int` | In | Options | Stop reading after this many rows. 0 means no limit. |
| Result table name | `string` | In | Options | Name given to the resulting DataTable. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Data table | `DataTable` | Out | Output | The rows the query returned, as a DataTable. |
| Row count | `int` | Out | Output | Number of rows in the result. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Execute Scalar

Runs a statement and returns a single value, for example the result of a COUNT or a MAX.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| SQL *(required)* | `string` | In | Input | The SQL statement. Use named parameters (@name) or question marks, never string concatenation. |
| Parameters | `Dictionary(Of String, Object)` | In | Input | Named parameters as a Dictionary(Of String, Object). The leading @ is optional. Keeps the query safe from SQL injection. |
| Parameter values | `IEnumerable(Of Object)` | In | Input | Values for the '?' placeholders of the statement, in order. An alternative to named parameters. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Value | `object` | Out | Output | The first value of the first row, as an Object. Use TextResult or NumberResult for a ready typed value. |
| Text result | `string` | Out | Output | The value as text, which saves a conversion in the workflow. |
| Number result | `double` | Out | Output | The value as a number. 0 when the value is NULL or not numeric. |
| Is null | `bool` | Out | Output | True when the query returned NULL or no row at all. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Execute Non Query

Runs an INSERT, UPDATE, DELETE or DDL statement. Writers are serialized through the writer lock.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| SQL *(required)* | `string` | In | Input | The SQL statement. Use named parameters (@name) or question marks, never string concatenation. |
| Parameters | `Dictionary(Of String, Object)` | In | Input | Named parameters as a Dictionary(Of String, Object). The leading @ is optional. Keeps the query safe from SQL injection. |
| Parameter values | `IEnumerable(Of Object)` | In | Input | Values for the '?' placeholders of the statement, in order. An alternative to named parameters. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Affected rows | `int` | Out | Output | How many rows the statement inserted, updated or deleted. |
| Last insert row id | `long` | Out | Output | ROWID of the row that was inserted last on this connection. 0 when the statement did not insert anything. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Insert Data Table

Writes a DataTable into a table in batches. Handles conflicts with ignore, replace or upsert, and can create the table from the DataTable.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Table name *(required)* | `string` | In | Input | Destination table. |
| Data table *(required)* | `DataTable` | In | Input | Rows to write. Column names must match the destination columns, unless a column mapping is supplied. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Conflict policy | `SQLiteConflictPolicy` | Value | Options | Abort fails on a constraint violation, Ignore skips the row, Replace overwrites it, Upsert updates the existing row (needs 'Key columns'). |
| Key columns | `IEnumerable(Of String)` | In | Options | Columns that identify an existing row for an UPSERT. They must be covered by a primary key or a unique index. |
| Update columns | `IEnumerable(Of String)` | In | Options | Columns updated when an UPSERT hits an existing row. Empty updates every non key column. |
| Batch size | `int` | In | Options | Rows per transaction. 0 writes everything in one transaction. Default 1000. |
| Column mapping | `Dictionary(Of String, String)` | In | Options | Maps DataTable column names onto table column names, as a Dictionary(Of String, String). |
| Create table if missing | `bool` | Value | Options | Create the destination table from the shape of the DataTable when it does not exist yet. |
| Ignore extra columns | `bool` | Value | Options | Silently skip DataTable columns that the destination table does not have. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Rows written | `int` | Out | Output | How many rows of the DataTable reached the table. |
| Processed rows | `int` | Out | Output | Rows that were sent to the database. |
| Skipped rows | `int` | Out | Output | Rows the database did not write, for example because of INSERT OR IGNORE. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Transaction Scope

Commits the activities inside as one unit of work and rolls everything back when one of them fails.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection to work on. Leave empty when the scope sits inside a SQLite Connect Scope. |
| Transaction mode | `SQLiteTransactionMode` | Value | Transaction | Immediate takes the write lock right away and is the safer choice when several robots write. Deferred waits for the first write. |
| Take writer lock | `bool` | Value | Transaction | Hold the writer lock for the whole transaction so that other processes queue up instead of failing with 'database is locked'. |
| Lock timeout (ms) | `int` | In | Transaction | How long the scope waits for the writer lock before it fails. |
| Committed | `bool` | Out | Output | True when the transaction was committed, false when it was rolled back. |

### SQLite Write Lock Scope

Takes the cross process writer lock once and keeps it for all the activities inside.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection whose database should be locked. Leave empty inside a SQLite Connect Scope. |
| Database path | `string` | In | Connection | Database to lock, when the scope is used without a connection. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines. Process: only inside this robot. |
| Lock file path | `string` | In | Locking | Lock file to use. Empty uses '<database file>.writelock'. |
| Lock timeout (ms) | `int` | In | Locking | How long the scope waits for the lock before it fails. |
| Waited (ms) | `long` | Out | Output | How long this scope had to wait before it got the lock. |

---

## SQLite > Export

Getting data out of the database, and CSV back in.

### SQLite Export To CSV

Writes a query result, a whole table or a DataTable to a CSV file, streaming row by row.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| SQL | `string` | In | Input | Query whose result is exported. Leave empty when 'Data table' is supplied. |
| Parameters | `Dictionary(Of String, Object)` | In | Input | Named parameters of the query, as a Dictionary(Of String, Object). |
| Table to export | `string` | In | Input | Name of a table to export completely. A simpler alternative to writing a SELECT. |
| Data table | `DataTable` | In | Input | Rows to export. When this is supplied, no query is run and no connection is needed. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| File path *(required)* | `string` | In | Output file | Where the CSV file is written. Missing folders are created. |
| Delimiter | `string` | In | Output file | Field separator. Default is a comma; use ";" or vbTab for other conventions. |
| Include headers | `bool` | Value | Output file | Write the column names as the first line. |
| Quote all fields | `bool` | Value | Output file | Wrap every field in quotes, not only the ones that need it. |
| Encoding | `string` | In | Output file | Encoding name, for example utf-8 (default), utf-16 or windows-1256. |
| Date format | `string` | In | Output file | Format applied to date and time values. Default yyyy-MM-dd HH:mm:ss. |
| Append | `bool` | Value | Output file | Append to an existing file instead of replacing it. Headers are only written for a new file. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| File written | `string` | Out | Output | Full path of the CSV file that was written. |
| Rows exported | `int` | Out | Output | Number of data rows written to the file. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Export To Excel

Writes a query result, a table or a DataTable to an .xlsx workbook. No Excel installation and no extra library needed.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Sheets | `Dictionary(Of String, String)` | In | Input | Several worksheets at once, as a Dictionary(Of String, String): the key is the sheet name, the value is the query. |
| SQL | `string` | In | Input | Query whose result is exported. Leave empty when 'Data table' is supplied. |
| Parameters | `Dictionary(Of String, Object)` | In | Input | Named parameters of the query, as a Dictionary(Of String, Object). |
| Table to export | `string` | In | Input | Name of a table to export completely. A simpler alternative to writing a SELECT. |
| Data table | `DataTable` | In | Input | Rows to export. When this is supplied, no query is run and no connection is needed. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| File path *(required)* | `string` | In | Output file | Where the .xlsx file is written. Missing folders are created. |
| Sheet name | `string` | In | Output file | Name of the worksheet. Default is Sheet1. |
| Include headers | `bool` | Value | Layout | Write the column names in the first row, in bold. |
| Freeze header row | `bool` | Value | Layout | Keep the header row visible while scrolling. |
| Auto filter | `bool` | Value | Layout | Add the filter dropdowns to the header row. |
| Auto size columns | `bool` | Value | Layout | Give the columns a width that fits the header and the first rows. |
| Date format | `string` | In | Layout | Excel number format for date and time values. Default yyyy-mm-dd hh:mm:ss. |
| Split large results | `bool` | Value | Layout | Spread a result larger than 1,048,576 rows over several worksheets instead of failing. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| File written | `string` | Out | Output | Full path of the workbook that was written. |
| Rows exported | `int` | Out | Output | Total number of data rows written to the workbook. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Export To JSON

Turns a query result into a JSON array of objects, written to a file or returned as text.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| SQL | `string` | In | Input | Query whose result is exported. Leave empty when 'Data table' is supplied. |
| Parameters | `Dictionary(Of String, Object)` | In | Input | Named parameters of the query, as a Dictionary(Of String, Object). |
| Table to export | `string` | In | Input | Name of a table to export completely. A simpler alternative to writing a SELECT. |
| Data table | `DataTable` | In | Input | Rows to export. When this is supplied, no query is run and no connection is needed. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| File path | `string` | In | Output file | Where the JSON file is written. Leave empty to get the JSON back as text in 'Result'. |
| Indented | `bool` | Value | Output file | Write the JSON with line breaks and indentation. |
| Encoding | `string` | In | Output file | Encoding name, for example utf-8 (default). |
| Date format | `string` | In | Output file | Format applied to date and time values. Default is the round trippable ISO 8601 format. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| File written | `string` | Out | Output | Full path of the JSON file, empty when only the Json output was asked for. |
| Rows exported | `int` | Out | Output | Number of rows written. |
| JSON | `string` | Out | Output | The JSON text. Also filled in when the result was written to a file, unless the result is very large. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Import CSV

Loads a CSV file into a table, in batches, with the same conflict handling as the bulk insert.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| File path *(required)* | `string` | In | Input | The CSV file to read. |
| Table name *(required)* | `string` | In | Input | Destination table. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Delimiter | `string` | In | File format | Field separator. Default is a comma. |
| Has headers | `bool` | Value | File format | The first line holds the column names. |
| Encoding | `string` | In | File format | Encoding name, for example utf-8 (default) or windows-1256. |
| Detect column types | `bool` | Value | File format | Turn numbers, dates and booleans into typed columns instead of importing everything as text. |
| Create table if missing | `bool` | Value | Options | Create the destination table from the columns of the file when it does not exist yet. |
| Conflict policy | `SQLiteConflictPolicy` | Value | Options | How to react to constraint violations. Upsert needs 'Key columns'. |
| Key columns | `IEnumerable(Of String)` | In | Options | Columns that identify an existing row for an UPSERT. |
| Batch size | `int` | In | Options | Rows per transaction. Default 1000. |
| Max rows | `int` | In | Options | Read at most this many rows from the file. 0 means all of them. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Rows imported | `int` | Out | Output | How many rows the CSV file added to the table. |
| Imported table | `DataTable` | Out | Output | The rows that were read from the file, handy for logging or checking. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

---

## SQLite > Schema

Asking the database about its own shape.

### SQLite Table Exists

Returns true when the given table or view exists in the database.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Table name *(required)* | `string` | In | Input | Name of the table or view to look for. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Exists | `bool` | Out | Output | True when the table, or the view, is there. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Get Table Names

Returns the names of the tables, and optionally the views, of the database.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Include views | `bool` | Value | Options | Also list views, not only tables. |
| Include internal tables | `bool` | Value | Options | Also list the internal sqlite_ tables. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Table names | `List(Of String)` | Out | Output | The table names found in the database, as a List(Of String). |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Get Table Schema

Returns the columns of a table as a DataTable: ordinal, name, declared type, not null, default value and primary key flag.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Table name *(required)* | `string` | In | Input | Table whose columns should be described. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Schema | `DataTable` | Out | Output | One row per column: ordinal, name, declared type, not null, default value and primary key flag. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Create Table

Creates a table from the shape of a DataTable, mapping .NET types onto SQLite storage classes.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Table name *(required)* | `string` | In | Input | Name of the table to create. |
| Data table *(required)* | `DataTable` | In | Input | DataTable whose columns describe the table to create. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Primary key columns | `IEnumerable(Of String)` | In | Options | Columns that form the primary key. Empty uses the primary key of the DataTable, if it has one. |
| Skip when it exists | `bool` | Value | Options | Use CREATE TABLE IF NOT EXISTS, so running the workflow twice is harmless. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Created | `bool` | Out | Output | True when the table was created, false when it already existed. |
| Statement | `string` | Out | Output | The CREATE TABLE statement that was executed. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

---

## SQLite > Maintenance

Housekeeping, backups and encryption.

### SQLite Backup Database

Creates a consistent copy of the database with the online backup API, safe to run while the database is in use.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Destination path *(required)* | `string` | In | Input | Where the copy is written. An existing file is replaced. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Backup password | `string` | In | Options | Password of the copy. Empty keeps the password of the source, so an encrypted database is backed up encrypted. |
| Overwrite | `bool` | Value | Options | Replace the destination file when it already exists. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Backup path | `string` | Out | Output | Full path of the copy that was written. |
| Size (bytes) | `long` | Out | Output | Size of the backup file that was written. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Set Password

Encrypts a database with AES-256 (SQLCipher), changes its password, or removes the encryption.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| New password | `string` | In | Input | The password to set. Leave it empty to decrypt the database and turn it back into a normal SQLite file. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Keep backup | `bool` | Value | Options | When the file has to be rewritten (encrypting or decrypting), keep the previous version as '<database>.bak'. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Database path | `string` | Out | Output | Full path of the database the new password was applied to. |
| Backup path | `string` | Out | Output | Where the previous version of the database was kept, or empty when the change was made in place. |
| Was encrypted | `bool` | Out | Output | True when the database was already encrypted before this activity ran. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Maintenance

Runs VACUUM, ANALYZE, PRAGMA optimize, a WAL checkpoint, an integrity check, a foreign key check or REINDEX.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Operation | `SQLiteMaintenanceOperation` | Value | Input | Which maintenance command to run. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Report | `string` | Out | Output | What the operation reported, for example the integrity check result. |
| Is healthy | `bool` | Out | Output | For the integrity and foreign key checks: true when the database reported no problem. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Attach Database

Attaches another database file to the connection under an alias, so queries can span both files.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Database to attach *(required)* | `string` | In | Input | Path of the database file to attach. |
| Alias *(required)* | `string` | In | Input | Name the attached database gets in SQL, for example 'archive'. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Detach instead | `bool` | Value | Options | Detach the alias again instead of attaching it. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Attached | `bool` | Out | Output | True when the database was attached, or detached. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

---

## SQLite > Advanced

Only when you have a specific reason.

### SQLite Connect Scope

Opens a SQLite database and keeps the connection open for the activities inside. No ODBC driver or SQLite installation is required.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Database path | `string` | In | Connection | Full path of the SQLite database file. The file, and its folder, are created when they do not exist yet. |
| Connection string | `string` | In | Connection | Complete connection string. Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | ReadWriteCreate creates the database when missing, ReadOnly never takes the writer lock, Memory keeps everything in RAM. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. Recommended. |
| Synchronous | `SQLiteSynchronousMode` | Value | Connection | Durability level. Normal is safe and fast together with WAL. |
| Enforce foreign keys | `bool` | Value | Connection | Turn foreign key constraints on. SQLite has them off by default. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Command timeout (s) | `int` | In | Connection | Default command timeout in seconds for the activities inside the scope. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Cache size (KB) | `int` | In | Connection | Page cache size in kilobytes. 0 keeps the engine default. |
| Retry attempts | `int` | In | Retry | How often a statement is retried while SQLite reports 'database is locked'. 1 disables retrying. |
| Retry initial delay (ms) | `int` | In | Retry | Delay before the first retry. It doubles after every failed attempt. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. |
| Lock timeout (ms) | `int` | In | Locking | How long a write activity waits for the writer lock before it fails. |
| Lock reads as well | `bool` | Value | Locking | Also take the lock for read activities. Only needed for non-WAL databases on a file share. |
| Connection | `SQLiteConnectionHandle` | Out | Output | The connection that was opened, in case you want to hand it to activities outside the scope. |
| SQLite version | `string` | Out | Output | Version of the embedded SQLite engine, for example 3.45.1. |
| Cipher version | `string` | Out | Output | Version of the SQLCipher layer, for example '4.5.2 community'. Empty when the loaded engine cannot do encryption. |

### SQLite Execute Script

Runs a multi statement SQL script from text or from a .sql file, optionally as one transaction.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Script | `string` | In | Input | The SQL script. Statements are separated by semicolons. Ignored when 'Script file path' is set. |
| Script file path | `string` | In | Input | Path of a .sql file to run. Wins over 'Script'. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Run in one transaction | `bool` | Value | Options | Wrap the whole script in one transaction, so a failure halfway leaves the database untouched. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Statements run | `int` | Out | Output | How many statements the script executed. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Execute Statements

Runs several write statements in one transaction while holding the writer lock once.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Statements | `IEnumerable(Of SQLiteStatement)` | In | Input | Statements with their parameters, as a List(Of SQLiteStatement). |
| SQL statements | `IEnumerable(Of String)` | In | Input | Plain statements without parameters, as a List(Of String). Runs after 'Statements'. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Run in one transaction | `bool` | Value | Options | Commit the whole batch together. When one statement fails, nothing is written. |
| Stop on first error | `bool` | Value | Options | Stop at the first failing statement. When false, the batch carries on and reports the failures. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Affected rows | `int` | Out | Output | Total number of rows affected by the whole batch. |
| Statements executed | `int` | Out | Output | How many statements ran successfully. |
| Failures | `List(Of String)` | Out | Output | Error message per failed statement, collected when 'Stop on first error' is off. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |

### SQLite Parallel Query

Runs several queries concurrently, each on its own read connection, and returns one DataTable per query.

| Property | Type | In/Out | Category | What it is |
| --- | --- | --- | --- | --- |
| Queries *(required)* | `Dictionary(Of String, String)` | In | Input | The queries to run, as a Dictionary(Of String, String): the key names the result, the value is the SELECT statement. |
| Connection | `SQLiteConnectionHandle` | In | Connection | Connection from SQLite Connect, or from an enclosing SQLite Connect Scope. Leave it empty to work straight from 'Database path' instead. |
| Database path | `string` | In | Connection | Full path of the SQLite database file, for example C:\Data\orders.db. Ignored when a connection or a connection string is supplied. |
| Connection string | `string` | In | Connection | Complete connection string, for example "Data Source=C:\Data\orders.db;Mode=ReadWriteCreate". Wins over 'Database path'. |
| Open mode | `SQLiteOpenMode` | Value | Connection | How the database file is opened when this activity opens its own connection. |
| Journal mode | `SQLiteJournalMode` | Value | Connection | WAL lets many readers work while one writer is active. This is what makes concurrent reads possible and is the recommended setting. |
| Busy timeout (ms) | `int` | In | Connection | How long SQLite waits for a lock held by another connection before it reports 'database is locked'. |
| Password | `string` | In | Connection | Password of an encrypted database. Leave it empty for a normal database. Use the SQLite Set Password activity to encrypt an existing database or to change its password. |
| Max parallel readers | `int` | In | Options | How many queries run at the same time. Default 4. |
| Open read only | `bool` | Value | Options | Open the extra connections read only, so they can never take the writer lock. |
| Column typing | `SQLiteColumnTyping` | Value | Options | Auto derives each column type from the values returned, Declared trusts the provider, AllText returns every column as text. |
| Lock scope | `SQLiteLockScope` | Value | Locking | Machine: a lock file serializes writers across processes and machines (default). Process: only inside this robot. None: rely on SQLite alone. |
| Lock file path | `string` | In | Locking | Lock file that serializes writers. Empty uses '<database file>.writelock'. Point several robots at the same file to serialize them. |
| Lock timeout (ms) | `int` | In | Locking | How long this activity waits for the writer lock before it fails. |
| TimeoutMS | `int` | In | Common | Maximum run time of this activity in milliseconds. Use 0 for no limit. |
| ContinueOnError | `bool` | In | Common | When true the workflow carries on even if this activity throws. The error is reported in 'ErrorMessage'. |
| Results | `Dictionary(Of String, DataTable)` | Out | Output | One DataTable per query, keyed by the name given to the query. |
| Total rows | `int` | Out | Output | Sum of the row counts of all results. |
| Error message | `string` | Out | Output | Message of the error that was swallowed because ContinueOnError is true. Empty when the activity succeeded. |
