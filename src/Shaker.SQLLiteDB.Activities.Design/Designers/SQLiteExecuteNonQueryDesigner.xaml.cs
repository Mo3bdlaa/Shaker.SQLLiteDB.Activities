using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Write.SQLiteExecuteNonQuery"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteExecuteNonQueryDesigner
    {
        public SQLiteExecuteNonQueryDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            SqlBox.ExpressionType = typeof(string);
            ResultBox.ExpressionType = typeof(int);
        }
    }
}
