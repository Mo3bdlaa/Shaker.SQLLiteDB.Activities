using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Query.SQLiteParallelQuery"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteParallelQueryDesigner
    {
        public SQLiteParallelQueryDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            QueriesBox.ExpressionType = typeof(System.Collections.Generic.IDictionary<string, string>);
            ResultBox.ExpressionType = typeof(System.Collections.Generic.Dictionary<string, System.Data.DataTable>);
        }
    }
}
