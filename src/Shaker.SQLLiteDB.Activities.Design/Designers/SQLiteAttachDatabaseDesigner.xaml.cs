using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteAttachDatabase"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteAttachDatabaseDesigner
    {
        public SQLiteAttachDatabaseDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            AttachPathBox.ExpressionType = typeof(string);
            AliasBox.ExpressionType = typeof(string);
        }
    }
}
