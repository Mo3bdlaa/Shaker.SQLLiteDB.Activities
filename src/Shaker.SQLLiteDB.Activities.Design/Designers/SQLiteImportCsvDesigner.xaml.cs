using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Export.SQLiteImportCsv"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteImportCsvDesigner
    {
        public SQLiteImportCsvDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            FilePathBox.ExpressionType = typeof(string);
            TableNameBox.ExpressionType = typeof(string);
            ResultBox.ExpressionType = typeof(int);
        }
    }
}
