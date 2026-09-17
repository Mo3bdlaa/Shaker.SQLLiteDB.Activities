using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Write.SQLiteBulkInsert"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteBulkInsertDesigner
    {
        public SQLiteBulkInsertDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            TableNameBox.ExpressionType = typeof(string);
            DataTableBox.ExpressionType = typeof(System.Data.DataTable);
            ResultBox.ExpressionType = typeof(int);
        }
    }
}
