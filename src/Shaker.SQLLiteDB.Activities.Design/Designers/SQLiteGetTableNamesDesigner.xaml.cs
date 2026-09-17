using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Schema.SQLiteGetTableNames"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteGetTableNamesDesigner
    {
        public SQLiteGetTableNamesDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            ResultBox.ExpressionType = typeof(System.Collections.Generic.List<string>);
        }
    }
}
