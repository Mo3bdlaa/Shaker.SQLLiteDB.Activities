using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Query.SQLiteExecuteQuery"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteExecuteQueryDesigner
    {
        public SQLiteExecuteQueryDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            SqlBox.ExpressionType = typeof(string);
            ResultBox.ExpressionType = typeof(System.Data.DataTable);
        }
    }
}
