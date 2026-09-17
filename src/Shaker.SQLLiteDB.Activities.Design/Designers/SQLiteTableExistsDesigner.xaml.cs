using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Schema.SQLiteTableExists"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteTableExistsDesigner
    {
        public SQLiteTableExistsDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            TableNameBox.ExpressionType = typeof(string);
            ResultBox.ExpressionType = typeof(bool);
        }
    }
}
