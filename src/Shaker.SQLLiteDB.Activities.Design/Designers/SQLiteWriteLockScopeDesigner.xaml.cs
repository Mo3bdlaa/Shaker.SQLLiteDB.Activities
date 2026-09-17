using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteWriteLockScope"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteWriteLockScopeDesigner
    {
        public SQLiteWriteLockScopeDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
        }
    }
}
