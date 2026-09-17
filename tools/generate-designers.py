#!/usr/bin/env python3
"""Regenerates src/Shaker.SQLLiteDB.Activities.Design/Designers.

One WPF ActivityDesigner per activity: the icon Studio shows in the panel, and the fields drawn on
the activity in the canvas. Which fields those are comes from PRINCIPAL, the same curated list the
view models use; the labels, hints and types come from the activity's own attributes, read out of
acts.json (see tools/dump-activity-metadata.cs).

    ACTS_JSON=/path/to/acts.json python3 tools/generate-designers.py
"""

import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'src', 'Shaker.SQLLiteDB.Activities.Design', 'Designers')
ACTS = {a['name']: a for a in json.load(open(os.environ.get('ACTS_JSON', 'acts.json')))}

# What each activity draws on the canvas, in order.
PRINCIPAL = {
  'SQLiteConnect':          ['DatabasePath', 'Password', 'Connection'],
  'SQLiteDisconnect':       ['Connection'],
  'SQLiteTransactionScope': ['Connection', 'TransactionMode'],
  'SQLiteConnectScope':     ['DatabasePath', 'Password', 'Connection'],
  'SQLiteWriteLockScope':   ['Connection', 'DatabasePath'],
  'SQLiteExecuteQuery':     ['Connection', 'DatabasePath', 'Sql', 'Result'],
  'SQLiteExecuteScalar':    ['Connection', 'DatabasePath', 'Sql', 'Result'],
  'SQLiteExecuteNonQuery':  ['Connection', 'DatabasePath', 'Sql', 'Result'],
  'SQLiteBulkInsert':       ['Connection', 'DatabasePath', 'TableName', 'DataTable', 'Result'],
  'SQLiteExecuteScript':    ['Connection', 'DatabasePath', 'Script', 'ScriptFilePath', 'Result'],
  'SQLiteExportToCsv':      ['Connection', 'Sql', 'SourceTableName', 'FilePath', 'Result'],
  'SQLiteExportToExcel':    ['Connection', 'Sql', 'SourceTableName', 'FilePath', 'Result'],
  'SQLiteExportToJson':     ['Connection', 'Sql', 'SourceTableName', 'FilePath', 'Json'],
  'SQLiteImportCsv':        ['Connection', 'DatabasePath', 'FilePath', 'TableName', 'Result'],
  'SQLiteTableExists':      ['Connection', 'DatabasePath', 'TableName', 'Result'],
  'SQLiteGetTableNames':    ['Connection', 'DatabasePath', 'Result'],
  'SQLiteGetTableSchema':   ['Connection', 'DatabasePath', 'TableName', 'Result'],
  'SQLiteCreateTable':      ['Connection', 'DatabasePath', 'TableName', 'DataTable', 'Result'],
  'SQLiteBackupDatabase':   ['Connection', 'DatabasePath', 'DestinationPath', 'Result'],
  'SQLiteSetPassword':      ['Connection', 'DatabasePath', 'NewPassword', 'Result'],
  'SQLiteMaintenance':      ['Connection', 'DatabasePath', 'Operation', 'Result'],
  'SQLiteAttachDatabase':   ['Connection', 'AttachPath', 'Alias'],
  'SQLiteParallelQuery':    ['Connection', 'DatabasePath', 'Queries', 'Result'],
  'SQLiteExecuteBatch':     ['Connection', 'DatabasePath', 'Statements', 'Result'],
}

CONNECTION, READ, WRITE, EXPORT, ADMIN = '#2F6FEB', '#16A34A', '#EA580C', '#7C3AED', '#475569'

# ---------------------------------------------------------------- icon drawing helpers
def fill(geometry):
    return '<GeometryDrawing Brush="#FFFFFF" Geometry="%s" />' % geometry

def stroke(geometry, thickness='1.5'):
    return ('<GeometryDrawing Geometry="%s"><GeometryDrawing.Pen>'
            '<Pen Brush="#FFFFFF" Thickness="%s" StartLineCap="Round" EndLineCap="Round" '
            'LineJoin="Round" /></GeometryDrawing.Pen></GeometryDrawing>') % (geometry, thickness)

def circle(cx, cy, r, filled=True, thickness='1.5'):
    element = ('<GeometryDrawing Brush="#FFFFFF">' if filled
               else '<GeometryDrawing><GeometryDrawing.Pen><Pen Brush="#FFFFFF" Thickness="%s" /></GeometryDrawing.Pen>' % thickness)
    return (element + '<GeometryDrawing.Geometry><EllipseGeometry Center="%s,%s" RadiusX="%s" RadiusY="%s" />'
            '</GeometryDrawing.Geometry></GeometryDrawing>') % (cx, cy, r, r)

def bar(x1, y, x2, thickness='1.5'):
    return stroke('M %s,%s L %s,%s' % (x1, y, x2, y), thickness)

CYLINDER = [
    '<GeometryDrawing Brush="#FFFFFF"><GeometryDrawing.Geometry><EllipseGeometry Center="8,4.6" RadiusX="4.2" RadiusY="1.7" /></GeometryDrawing.Geometry></GeometryDrawing>',
    fill('M 3.8,4.6 L 12.2,4.6 L 12.2,11 A 4.2,1.7 0 0 1 3.8,11 Z'),
]
ARROW_RIGHT = [stroke('M 8.5,8 L 13,8'), fill('M 11.6,5.9 L 14.4,8 L 11.6,10.1 Z')]
ARROW_DOWN = [stroke('M 8,2.5 L 8,7'), fill('M 5.9,5.8 L 8,8.6 L 10.1,5.8 Z')]
PAGE = [fill('M 3.2,2.2 L 9,2.2 L 11.6,4.8 L 11.6,13.8 L 3.2,13.8 Z')]
GRID = [fill('M 3,3 L 7.4,3 L 7.4,7.4 L 3,7.4 Z'), fill('M 8.6,3 L 13,3 L 13,7.4 L 8.6,7.4 Z'),
        fill('M 3,8.6 L 7.4,8.6 L 7.4,13 L 3,13 Z'), fill('M 8.6,8.6 L 13,8.6 L 13,13 L 8.6,13 Z')]
MAGNIFIER = [circle(7, 7, 3.6, filled=False, thickness='1.6'), stroke('M 9.8,9.8 L 13.2,13.2', '1.8')]
PADLOCK = [stroke('M 5.4,7 L 5.4,5.4 A 2.6,2.6 0 0 1 10.6,5.4 L 10.6,7', '1.5'),
           fill('M 4,7.2 L 12,7.2 L 12,13.4 L 4,13.4 Z')]
PENCIL = [fill('M 3,13 L 4,10.2 L 10.6,3.6 L 12.4,5.4 L 5.8,12 Z'),
          fill('M 11.2,3 L 13,4.8 L 11.9,5.9 L 10.1,4.1 Z')]
PLUG = [bar('3', '8', '6.6', '1.8'), fill('M 6.6,5.4 L 10.4,5.4 L 10.4,10.6 L 6.6,10.6 Z'),
        bar('10.4', '6.6', '13'), bar('10.4', '9.4', '13')]

ICONS = {
  'SQLiteConnect':          (CONNECTION, PLUG),
  'SQLiteDisconnect':       (CONNECTION, [bar('2.6', '8', '5.6', '1.8'),
                                          fill('M 8.6,5.4 L 12.4,5.4 L 12.4,10.6 L 8.6,10.6 Z'),
                                          stroke('M 3.4,12.6 L 12.6,3.4', '1.6')]),
  'SQLiteConnectScope':     (CONNECTION, [stroke('M 4,3 L 2.2,3 L 2.2,13 L 4,13'),
                                          stroke('M 12,3 L 13.8,3 L 13.8,13 L 12,13'),
                                          fill('M 6,5.6 L 9.4,5.6 L 9.4,10.4 L 6,10.4 Z'),
                                          bar('9.4', '8', '11')]),
  'SQLiteTransactionScope': (CONNECTION, [stroke('M 12.6,8 A 4.6,4.6 0 1 1 8,3.4', '1.6'),
                                          fill('M 6.2,1.4 L 9.4,3.4 L 6.2,5.4 Z')]),
  'SQLiteWriteLockScope':   (CONNECTION, PADLOCK),

  'SQLiteExecuteQuery':     (READ, MAGNIFIER),
  'SQLiteExecuteScalar':    (READ, [circle(8, 6.4, 3.2, filled=False, thickness='1.6'), bar('4.4', '12.4', '11.6', '1.8')]),
  'SQLiteParallelQuery':    (READ, [circle(5.6, 6.2, 2.8, filled=False, thickness='1.5'),
                                    stroke('M 7.7,8.3 L 9.6,10.2', '1.6'),
                                    circle(11.2, 10.4, 2.4, filled=False, thickness='1.5')]),
  'SQLiteTableExists':      (READ, [fill('M 2.6,3 L 13.4,3 L 13.4,6 L 2.6,6 Z'),
                                    stroke('M 3.6,9.6 L 6.4,12.4 L 12.4,6.4', '2')]),
  'SQLiteGetTableNames':    (READ, [bar('3', '4.4', '13', '1.8'), bar('3', '8', '13', '1.8'), bar('3', '11.6', '9.6', '1.8')]),
  'SQLiteGetTableSchema':   (READ, [fill('M 2.6,3 L 6,3 L 6,13 L 2.6,13 Z'),
                                    fill('M 7.3,3 L 10.7,3 L 10.7,13 L 7.3,13 Z'),
                                    fill('M 12,3 L 13.4,3 L 13.4,13 L 12,13 Z')]),

  'SQLiteExecuteNonQuery':  (WRITE, PENCIL),
  'SQLiteBulkInsert':       (WRITE, [fill('M 2.6,9.6 L 13.4,9.6 L 13.4,13.4 L 2.6,13.4 Z'),
                                     stroke('M 8,1.6 L 8,6', '1.8'), fill('M 5.4,4.8 L 8,8.2 L 10.6,4.8 Z')]),
  'SQLiteExecuteScript':    (WRITE, PAGE + [stroke('M 5,7 L 9.8,7', '1.2'), stroke('M 5,9.2 L 9.8,9.2', '1.2'),
                                            stroke('M 5,11.4 L 7.8,11.4', '1.2')]),
  'SQLiteExecuteBatch':     (WRITE, [fill('M 2.6,3 L 13.4,3 L 13.4,5.4 L 2.6,5.4 Z'),
                                     fill('M 2.6,6.8 L 10.6,6.8 L 10.6,9.2 L 2.6,9.2 Z'),
                                     fill('M 2.6,10.6 L 13.4,10.6 L 13.4,13 L 2.6,13 Z')]),
  'SQLiteCreateTable':      (WRITE, [fill('M 2.6,3 L 13.4,3 L 13.4,6 L 2.6,6 Z'),
                                     fill('M 2.6,7.2 L 8,7.2 L 8,13 L 2.6,13 Z'),
                                     stroke('M 11.4,8 L 11.4,13.4', '1.9'), stroke('M 8.7,10.7 L 14.1,10.7', '1.9')]),
  'SQLiteImportCsv':        (WRITE, PAGE + [stroke('M 13.6,9 L 8.6,9', '1.6'), fill('M 10.2,7 L 7.4,9 L 10.2,11 Z')]),

  'SQLiteExportToCsv':      (EXPORT, PAGE + ARROW_RIGHT),
  'SQLiteExportToExcel':    (EXPORT, GRID),
  'SQLiteExportToJson':     (EXPORT, [stroke('M 6.4,2.6 C 4,2.6 5.2,7.2 2.8,8 C 5.2,8.8 4,13.4 6.4,13.4', '1.5'),
                                      stroke('M 9.6,2.6 C 12,2.6 10.8,7.2 13.2,8 C 10.8,8.8 12,13.4 9.6,13.4', '1.5')]),

  'SQLiteBackupDatabase':   (ADMIN, CYLINDER[:1] + [fill('M 3.8,4.6 L 12.2,4.6 L 12.2,8.4 A 4.2,1.7 0 0 1 3.8,8.4 Z')] +
                                    [stroke('M 8,9.4 L 8,12.6', '1.6'), fill('M 5.8,11.6 L 8,14.2 L 10.2,11.6 Z')]),
  'SQLiteSetPassword':      (ADMIN, [circle(5.4, 6.2, 2.9, filled=False, thickness='1.7'),
                                     stroke('M 7.4,8.2 L 13,13.8', '1.7'), stroke('M 11.2,12 L 12.8,10.4', '1.6')]),
  'SQLiteMaintenance':      (ADMIN, [circle(8, 8, 2.2, filled=False, thickness='1.6'),
                                     bar('7.1', '2.2', '8.9', '2.2'), bar('7.1', '13.8', '8.9', '2.2'),
                                     stroke('M 2.2,7.1 L 2.2,8.9', '2.2'), stroke('M 13.8,7.1 L 13.8,8.9', '2.2'),
                                     stroke('M 4.2,4.2 L 5,5', '2'), stroke('M 11,11 L 11.8,11.8', '2'),
                                     stroke('M 11.8,4.2 L 11,5', '2'), stroke('M 5,11 L 4.2,11.8', '2')]),
  'SQLiteAttachDatabase':   (ADMIN, [fill('M 2.4,3.4 L 9.2,3.4 L 9.2,10.2 L 2.4,10.2 Z'),
                                     stroke('M 6.8,5.8 L 13.6,5.8 L 13.6,12.6 L 6.8,12.6 Z', '1.6')]),
}

TILE = 'M 3,0 L 13,0 A 3,3 0 0 1 16,3 L 16,13 A 3,3 0 0 1 13,16 L 3,16 A 3,3 0 0 1 0,13 L 0,3 A 3,3 0 0 1 3,0 Z'

def icon_xaml(name, indent='    '):
    colour, glyph = ICONS[name]
    drawings = ['<GeometryDrawing Brush="%s" Geometry="%s" />' % (colour, TILE)] + glyph
    body = ('\n' + indent + '          ').join(drawings)
    return ('%(i)s<sap:ActivityDesigner.Icon>\n'
            '%(i)s  <DrawingBrush Stretch="Uniform">\n'
            '%(i)s    <DrawingBrush.Drawing>\n'
            '%(i)s      <DrawingGroup>\n'
            '%(i)s          %(body)s\n'
            '%(i)s      </DrawingGroup>\n'
            '%(i)s    </DrawingBrush.Drawing>\n'
            '%(i)s  </DrawingBrush>\n'
            '%(i)s</sap:ActivityDesigner.Icon>') % dict(i=indent, body=body)

# ---------------------------------------------------------------- body rows
def escape(text):
    return (text.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')
                .replace('"', '&quot;'))

def hint_for(prop):
    tip = prop['tooltip'].split('.')[0].strip()
    return tip if 0 < len(tip) <= 70 else prop['display'] or prop['name']

def generate(name):
    act = ACTS[name]
    props = {p['name']: p for p in act['props']}
    rows, setup, extra_ns = [], [], set()

    for index, field in enumerate(PRINCIPAL[name]):
        prop = props[field]
        label = prop['display'] or field
        if prop['kind'] in ('in', 'out'):
            direction = 'In' if prop['kind'] == 'in' else 'Out'
            location = ' UseLocationExpression="True"' if direction == 'Out' else ''
            rows.append(
                '    <TextBlock Grid.Row="%d" Grid.Column="0" Text="%s" Margin="0,5,8,5" VerticalAlignment="Center" />\n'
                '    <sapv:ExpressionTextBox x:Name="%sBox" Grid.Row="%d" Grid.Column="1" Margin="0,2"\n'
                '                            HintText="%s"%s\n'
                '                            OwnerActivity="{Binding Path=ModelItem}"\n'
                '                            Expression="{Binding Path=ModelItem.%s, Mode=TwoWay, Converter={StaticResource ArgumentToExpressionConverter}, ConverterParameter=%s}" />'
                % (index, escape(label), field, index, escape(hint_for(prop)), location, field, direction))
            setup.append('            %sBox.ExpressionType = typeof(%s);' % (field, prop['type']))
        elif prop['isEnum']:
            rows.append(
                '    <TextBlock Grid.Row="%d" Grid.Column="0" Text="%s" Margin="0,5,8,5" VerticalAlignment="Center" />\n'
                '    <ComboBox x:Name="%sBox" Grid.Row="%d" Grid.Column="1" Margin="0,2" HorizontalAlignment="Left" MinWidth="150"\n'
                '              SelectedValue="{Binding Path=ModelItem.%s, Mode=TwoWay}" />'
                % (index, escape(label), field, index, field))
            setup.append('            %sBox.ItemsSource = Enum.GetValues(typeof(%s));' % (field, prop['type']))
        else:
            rows.append(
                '    <CheckBox Grid.Row="%d" Grid.Column="1" Margin="0,4" Content="%s"\n'
                '              IsChecked="{Binding Path=ModelItem.%s, Mode=TwoWay}" />'
                % (index, escape(label), field))

    row_defs = '\n'.join('      <RowDefinition Height="Auto" />' for _ in PRINCIPAL[name])
    xaml = '''<sap:ActivityDesigner x:Class="Shaker.SQLLiteDB.Activities.Design.Designers.%(cls)s"
                      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                      xmlns:sap="clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation"
                      xmlns:sapv="clr-namespace:System.Activities.Presentation.View;assembly=System.Activities.Presentation"
                      xmlns:sapc="clr-namespace:System.Activities.Presentation.Converters;assembly=System.Activities.Presentation">
%(icon)s
  <sap:ActivityDesigner.Resources>
    <sapc:ArgumentToExpressionConverter x:Key="ArgumentToExpressionConverter" />
  </sap:ActivityDesigner.Resources>
  <Grid Margin="4">
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="Auto" />
      <ColumnDefinition Width="*" MinWidth="190" />
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
%(rows_def)s
    </Grid.RowDefinitions>
%(rows)s
  </Grid>
</sap:ActivityDesigner>
''' % dict(cls=name + 'Designer', icon=icon_xaml(name), rows_def=row_defs, rows='\n'.join(rows))

    code = '''using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="%(ns)s.%(name)s"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class %(cls)s
    {
        public %(cls)s()
        {
            InitializeComponent();
%(setup)s
        }
    }
}
''' % dict(ns=act['ns'], name=name, cls=name + 'Designer', setup='\n'.join(setup) or '            // nothing further to wire up')
    return xaml, code


if os.path.isdir(OUT):
    for stale in os.listdir(OUT):
        os.remove(os.path.join(OUT, stale))
os.makedirs(OUT, exist_ok=True)

registrations = []
for name in sorted(ACTS):
    xaml, code = generate(name)
    open(os.path.join(OUT, name + 'Designer.xaml'), 'w').write(xaml)
    open(os.path.join(OUT, name + 'Designer.xaml.cs'), 'w').write(code)
    registrations.append((ACTS[name]['ns'], name))

lines = '\n'.join(
    '            builder.AddCustomAttributes(typeof(%s.%s), new DesignerAttribute(typeof(Designers.%sDesigner)));'
    % (ns, n, n) for ns, n in registrations)
open(os.path.join(os.path.dirname(OUT), 'DesignerMetadata.cs'), 'w').write('''using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Studio calls this when it loads the package, and it is how each activity finds its designer -
    /// the icon in the activities panel and the fields drawn on the activity in the canvas. Attaching
    /// the designers here rather than with a [Designer] attribute on the activities keeps the runtime
    /// assembly free of any reference to WPF, so a robot never loads any of this.
    /// </summary>
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();

%s

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
''' % lines)

print('%d designers' % len(registrations))
