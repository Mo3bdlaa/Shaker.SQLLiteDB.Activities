using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Write.SQLiteExecuteBatch"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteExecuteBatchDesigner
    {
        public SQLiteExecuteBatchDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            StatementsBox.ExpressionType = typeof(System.Collections.Generic.IEnumerable<Shaker.SQLLiteDB.Activities.Activities.SQLiteStatement>);
            ResultBox.ExpressionType = typeof(int);
        }
    }
}
