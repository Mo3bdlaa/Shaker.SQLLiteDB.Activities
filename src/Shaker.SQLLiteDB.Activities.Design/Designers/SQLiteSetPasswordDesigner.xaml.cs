using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteSetPassword"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteSetPasswordDesigner
    {
        public SQLiteSetPasswordDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            NewPasswordBox.ExpressionType = typeof(string);
            ResultBox.ExpressionType = typeof(string);
        }
    }
}
