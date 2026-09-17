using System;

namespace Shaker.SQLLiteDB.Activities.Design.Designers
{
    /// <summary>Canvas layout and panel icon for <see cref="Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteMaintenance"/>. Generated - see tools/generate-designers.py.</summary>
    public partial class SQLiteMaintenanceDesigner
    {
        public SQLiteMaintenanceDesigner()
        {
            InitializeComponent();
            ConnectionBox.ExpressionType = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle);
            DatabasePathBox.ExpressionType = typeof(string);
            OperationBox.ItemsSource = Enum.GetValues(typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteMaintenanceOperation));
            ResultBox.ExpressionType = typeof(string);
        }
    }
}
