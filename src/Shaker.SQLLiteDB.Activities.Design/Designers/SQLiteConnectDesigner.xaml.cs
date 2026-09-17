using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteConnect"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteConnectDesigner
    {
        public SQLiteConnectDesigner()
        {
            InitializeComponent();
            DatabasePathBox.ExpressionType = typeof(string);
            PasswordBox.ExpressionType = typeof(string);
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
        }
    }
}
