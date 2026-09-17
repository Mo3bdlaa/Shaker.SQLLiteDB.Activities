using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Write.SQLiteExecuteScript"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteExecuteScriptDesigner
    {
        public SQLiteExecuteScriptDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            ScriptBox.ExpressionType = typeof(string);
            ScriptFilePathBox.ExpressionType = typeof(string);
            ResultBox.ExpressionType = typeof(int);
        }
    }
}
