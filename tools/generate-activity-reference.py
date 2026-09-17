#!/usr/bin/env python3
"""Writes docs/activities.md: every activity, every property, straight from the built assembly.

    ACTS_JSON=/path/to/acts.json python3 tools/generate-activity-reference.py
"""

import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ACTS = {a['name']: a for a in json.load(open(os.environ.get('ACTS_JSON', 'acts.json')))}

ORDER = [
  ('SQLite', 'The everyday ones, at the top level of the panel.',
   ['SQLiteConnect', 'SQLiteDisconnect', 'SQLiteExecuteQuery', 'SQLiteExecuteScalar',
    'SQLiteExecuteNonQuery', 'SQLiteBulkInsert', 'SQLiteTransactionScope', 'SQLiteWriteLockScope']),
  ('SQLite > Export', 'Getting data out of the database, and CSV back in.',
   ['SQLiteExportToCsv', 'SQLiteExportToExcel', 'SQLiteExportToJson', 'SQLiteImportCsv']),
  ('SQLite > Schema', 'Asking the database about its own shape.',
   ['SQLiteTableExists', 'SQLiteGetTableNames', 'SQLiteGetTableSchema', 'SQLiteCreateTable']),
  ('SQLite > Maintenance', 'Housekeeping, backups and encryption.',
   ['SQLiteBackupDatabase', 'SQLiteSetPassword', 'SQLiteMaintenance', 'SQLiteAttachDatabase']),
  ('SQLite > Advanced', 'Only when you have a specific reason.',
   ['SQLiteConnectScope', 'SQLiteExecuteScript', 'SQLiteExecuteBatch', 'SQLiteParallelQuery']),
]

CATEGORY_ORDER = ['Input', 'Connection', 'Output file', 'File format', 'Layout', 'Transaction',
                  'Options', 'Retry', 'Locking', 'Common', 'Output']

SHORT = {
  'System.Data.DataTable': 'DataTable',
  'System.Collections.Generic.IDictionary<string, object>': 'Dictionary(Of String, Object)',
  'System.Collections.Generic.IDictionary<string, string>': 'Dictionary(Of String, String)',
  'System.Collections.Generic.IEnumerable<string>': 'IEnumerable(Of String)',
  'System.Collections.Generic.IEnumerable<object>': 'IEnumerable(Of Object)',
  'System.Collections.Generic.List<string>': 'List(Of String)',
  'System.Collections.Generic.Dictionary<string, System.Data.DataTable>': 'Dictionary(Of String, DataTable)',
  'System.Collections.Generic.IEnumerable<Shaker.SQLLiteDB.Activities.Activities.SQLiteStatement>': 'IEnumerable(Of SQLiteStatement)',
}

def type_name(prop):
    name = SHORT.get(prop['type'], prop['type'].split('.')[-1])
    if prop['kind'] == 'in':
        return name
    if prop['kind'] == 'out':
        return name
    return name

def cell(text):
    return text.replace('|', '\\|').replace('\n', ' ').strip()

def sort_key(prop):
    category = prop['category'] or 'Options'
    index = CATEGORY_ORDER.index(category) if category in CATEGORY_ORDER else len(CATEGORY_ORDER)
    return (index, 0 if prop['required'] else 1)

lines = ['# Activity reference', '',
         'Every activity and every property it has. Generated from the assembly itself, so it cannot',
         'drift from what Studio shows you.', '',
         'The **In/Out** column says whether a property is an argument you can bind to a variable or',
         'expression (In / Out) or a plain value you pick in the Properties panel (Value). Properties',
         'marked **required** must be set for the activity to validate.', '']

for group, blurb, names in ORDER:
    lines += ['---', '', '## %s' % group, '', blurb, '']
    for name in names:
        act = ACTS[name]
        lines += ['### %s' % act['display'], '']
        doc = act.get('description') or ''
        if doc:
            lines += [doc, '']
        props = sorted(act['props'], key=sort_key)
        lines += ['| Property | Type | In/Out | Category | What it is |',
                  '| --- | --- | --- | --- | --- |']
        for prop in props:
            direction = {'in': 'In', 'out': 'Out'}.get(prop['kind'], 'Value')
            label = cell(prop['display'] or prop['name'])
            if prop['required']:
                label += ' *(required)*'
            lines.append('| %s | `%s` | %s | %s | %s |'
                         % (label, type_name(prop), direction,
                            prop['category'] or 'Options', cell(prop['tooltip'])))
        lines.append('')

out = os.path.join(ROOT, 'docs')
os.makedirs(out, exist_ok=True)
open(os.path.join(out, 'activities.md'), 'w').write('\n'.join(lines).rstrip() + '\n')
print('docs/activities.md: %d activities' % sum(len(n) for _, _, n in ORDER))
