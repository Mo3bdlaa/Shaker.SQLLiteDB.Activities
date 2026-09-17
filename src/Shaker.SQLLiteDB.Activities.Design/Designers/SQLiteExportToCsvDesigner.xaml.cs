using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Export.SQLiteExportToCsv"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteExportToCsvDesigner
    {
        public SQLiteExportToCsvDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            SqlBox.ExpressionType = typeof(string);
            SourceTableNameBox.ExpressionType = typeof(string);
            FilePathBox.ExpressionType = typeof(string);
            ResultBox.ExpressionType = typeof(string);
        }
    }
}
