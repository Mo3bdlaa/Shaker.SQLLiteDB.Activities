#!/usr/bin/env python3
"""Regenerates src/Shaker.SQLLiteDB.Activities/Design from the built activity assembly.

Every view model has to list every property of its activity: the Properties panel is built from
the view model, so anything left out disappears from the designer altogether. That is too much to
keep in sync by hand across 24 activities, so it is generated instead.

    dotnet build src/Shaker.SQLLiteDB.Activities -c Release -f net6.0
    dotnet run --project <scratch project using tools/dump-activity-metadata.cs> -- acts.json
    python3 tools/generate-viewmodels.py

The only thing curated here is PRINCIPAL (which fields are drawn inside the activity, in order)
and RESULT_NAME (a readable name for the generic Result output). Everything else - display names,
tooltips, categories, required flags - comes from the attributes already on the activity.
"""

import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ACTS = json.load(open(os.environ.get('ACTS_JSON', 'acts.json')))
OUT = os.path.join(ROOT, 'src', 'Shaker.SQLLiteDB.Activities', 'Design')

# Ordered list of the fields drawn inside the activity body, per activity.
PRINCIPAL = {
  'SQLiteConnect':          ['DatabasePath', 'Password', 'Connection'],
  'SQLiteDisconnect':       ['Connection', 'Result'],
  'SQLiteTransactionScope': ['Connection', 'TransactionMode'],
  'SQLiteConnectScope':     ['DatabasePath', 'Password', 'Connection'],
  'SQLiteWriteLockScope':   ['Connection', 'DatabasePath'],
  'SQLiteExecuteQuery':     ['Connection', 'DatabasePath', 'Sql', 'Parameters', 'Result'],
  'SQLiteExecuteScalar':    ['Connection', 'DatabasePath', 'Sql', 'Parameters', 'Result'],
  'SQLiteExecuteNonQuery':  ['Connection', 'DatabasePath', 'Sql', 'Parameters', 'Result'],
  'SQLiteBulkInsert':       ['Connection', 'DatabasePath', 'TableName', 'DataTable', 'ConflictPolicy', 'Result'],
  'SQLiteExecuteScript':    ['Connection', 'DatabasePath', 'Script', 'ScriptFilePath', 'Result'],
  'SQLiteExportToCsv':      ['Connection', 'DatabasePath', 'Sql', 'SourceTableName', 'DataTable', 'FilePath', 'Result'],
  'SQLiteExportToExcel':    ['Connection', 'DatabasePath', 'Sql', 'SourceTableName', 'DataTable', 'FilePath', 'Result'],
  'SQLiteImportCsv':        ['Connection', 'DatabasePath', 'FilePath', 'TableName', 'Result'],
  'SQLiteTableExists':      ['Connection', 'DatabasePath', 'TableName', 'Result'],
  'SQLiteGetTableNames':    ['Connection', 'DatabasePath', 'Result'],
  'SQLiteBackupDatabase':   ['Connection', 'DatabasePath', 'DestinationPath', 'Result'],
  'SQLiteSetPassword':      ['Connection', 'DatabasePath', 'NewPassword', 'Result'],
  'SQLiteMaintenance':      ['Connection', 'DatabasePath', 'Operation', 'Result'],
  'SQLiteParallelQuery':    ['Connection', 'DatabasePath', 'Queries', 'Result'],
  'SQLiteExecuteBatch':     ['Connection', 'DatabasePath', 'Statements', 'SqlStatements', 'Result'],
  'SQLiteAttachDatabase':   ['Connection', 'DatabasePath', 'AttachPath', 'Alias', 'Result'],
  'SQLiteCreateTable':      ['Connection', 'DatabasePath', 'TableName', 'DataTable', 'Result'],
  'SQLiteGetTableSchema':   ['Connection', 'DatabasePath', 'TableName', 'Result'],
  'SQLiteExportToJson':     ['Connection', 'DatabasePath', 'Sql', 'SourceTableName', 'FilePath', 'Result'],
}

# "Result" says nothing about what came back, so every activity names its own.
RESULT_NAME = {
  'SQLiteDisconnect':      ('Closed',         'True when the connection was open and is now closed.'),
  'SQLiteExecuteQuery':    ('Data table',     'The rows the query returned.'),
  'SQLiteExecuteScalar':   ('Value',          'The first value of the first row.'),
  'SQLiteExecuteNonQuery': ('Affected rows',  'How many rows the statement inserted, updated or deleted.'),
  'SQLiteBulkInsert':      ('Rows written',   'How many rows reached the table.'),
  'SQLiteExecuteScript':   ('Statements run', 'How many statements the script executed.'),
  'SQLiteExportToCsv':     ('File written',   'Full path of the CSV file.'),
  'SQLiteExportToExcel':   ('File written',   'Full path of the workbook.'),
  'SQLiteExportToJson':    ('File written',   'Full path of the JSON file, empty when only the Json output was asked for.'),
  'SQLiteImportCsv':       ('Rows imported',  'How many rows the CSV added to the table.'),
  'SQLiteTableExists':     ('Exists',         'True when the table (or view) is there.'),
  'SQLiteGetTableNames':   ('Table names',    'The names found in the database.'),
  'SQLiteGetTableSchema':  ('Schema',         'One row per column: ordinal, name, declared type, not null, default and primary key.'),
  'SQLiteCreateTable':     ('Created',        'True when the table was created, false when it already existed.'),
  'SQLiteBackupDatabase':  ('Backup path',    'Full path of the copy that was written.'),
  'SQLiteSetPassword':     ('Database path',  'Full path of the database the password was applied to.'),
  'SQLiteMaintenance':     ('Report',         'What the operation reported.'),
  'SQLiteAttachDatabase':  ('Attached',       'True when the database was attached (or detached).'),
  'SQLiteParallelQuery':   ('Results',        'One DataTable per query, keyed by the name you gave it.'),
  'SQLiteExecuteBatch':    ('Affected rows',  'Total rows affected by the whole batch.'),
}

# Where a property sits in the Properties panel when the activity does not say.
CATEGORY_ORDER = ['Input', 'Connection', 'Output file', 'File format', 'Layout', 'Transaction',
                  'Options', 'Retry', 'Locking', 'Common', 'Output']

TEXT_AREAS = {'Sql', 'Script', 'Statements', 'SqlStatements', 'Queries'}

def widget(p):
    if p['isEnum']:
        return 'Dropdown'
    if p['name'] in TEXT_AREAS and p['kind'] == 'in' and p['type'] == 'string':
        return 'TextComposer'
    if p['isBool']:
        return 'NullableBoolean' if p['kind'] == 'in' else 'Checkbox'
    return 'Input'

def design_type(p):
    if p['kind'] == 'in':
        return 'DesignInArgument<%s>' % p['type']
    if p['kind'] == 'out':
        return 'DesignOutArgument<%s>' % p['type']
    return 'DesignProperty<%s>' % p['type']

def cs(s):
    return '"%s"' % s.replace('\\', '\\\\').replace('"', '\\"')

def sort_key(name, p, principal):
    if name in principal:
        return (0, principal.index(name))
    cat = p['category'] or ('Output' if name == 'Result' else 'Options')
    idx = CATEGORY_ORDER.index(cat) if cat in CATEGORY_ORDER else len(CATEGORY_ORDER)
    return (1, idx)

os.makedirs(OUT, exist_ok=True)
for old in os.listdir(OUT):
    os.remove(os.path.join(OUT, old))

written = []
for act in ACTS:
    name = act['name']
    principal = PRINCIPAL[name]
    props = {p['name']: p for p in act['props']}
    missing = [n for n in principal if n not in props]
    assert not missing, (name, missing)
    order = sorted(props, key=lambda n: sort_key(n, props[n], principal))

    decls, init = [], []
    for i, n in enumerate(order, start=1):
        p = props[n]
        decls.append('        public %s %s { get; set; } = new %s();' % (design_type(p), n, design_type(p)))
        display, tooltip = p['display'], p['tooltip']
        if n == 'Result' and name in RESULT_NAME:
            display, tooltip = RESULT_NAME[name]
        lines = []
        if display:
            lines.append('%s.DisplayName = %s;' % (n, cs(display)))
        if tooltip:
            lines.append('%s.Tooltip = %s;' % (n, cs(tooltip)))
        cat = p['category'] or ('Output' if n == 'Result' else 'Options')
        lines.append('%s.Category = %s;' % (n, cs(cat)))
        if n in principal:
            lines.append('%s.IsPrincipal = true;' % n)
        if p['required']:
            lines.append('%s.IsRequired = true;' % n)
        lines.append('%s.OrderIndex = order++;' % n)
        w = widget(p)
        lines.append('%s.Widget = new DefaultWidget { Type = "%s" };' % (n, w))
        if w == 'Dropdown':
            lines.append('%s.DataSource = EnumDataSourceBuilder<%s>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());' % (n, p['type']))
        init.append('\n'.join('            ' + l for l in lines))

    vm = name + 'ViewModel'
    body = '''#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="%(ns)s.%(name)s"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class %(vm)s : DesignPropertiesViewModel
    {
        public %(vm)s(IDesignServices services)
            : base(services)
        {
        }

%(decls)s

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var order = 1;

%(init)s
        }
    }
}
#endif
''' % dict(ns=act['ns'], name=name, vm=vm, decls='\n\n'.join(decls), init='\n\n'.join(init))
    open(os.path.join(OUT, vm + '.cs'), 'w').write(body)
    written.append((act['ns'], name, vm))

print('%d view models' % len(written))
