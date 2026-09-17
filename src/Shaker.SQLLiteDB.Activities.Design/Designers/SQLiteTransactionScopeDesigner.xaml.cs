using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteTransactionScope"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteTransactionScopeDesigner
    {
        public SQLiteTransactionScopeDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            TransactionModeBox.ItemsSource = Enum.GetValues(typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteTransactionMode));
        }
    }
}
