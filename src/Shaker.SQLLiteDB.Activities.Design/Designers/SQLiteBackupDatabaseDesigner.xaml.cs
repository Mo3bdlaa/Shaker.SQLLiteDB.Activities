using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteBackupDatabase"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteBackupDatabaseDesigner
    {
        public SQLiteBackupDatabaseDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            DestinationPathBox.ExpressionType = typeof(string);
            ResultBox.ExpressionType = typeof(string);
        }
    }
}
