using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Studio calls this when it loads the package, and it is how each activity finds its designer -
    /// the icon in the activities panel and the fields drawn on the activity in the canvas. Attaching
    /// the designers here rather than with a [Designer] attribute on the activities keeps the runtime
    /// assembly free of any reference to WPF, so a robot never loads any of this.
    /// </summary>
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();

            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteAttachDatabase), new DesignerAttribute(typeof(Designers.SQLiteAttachDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteBackupDatabase), new DesignerAttribute(typeof(Designers.SQLiteBackupDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Write.SQLiteBulkInsert), new DesignerAttribute(typeof(Designers.SQLiteBulkInsertDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteConnect), new DesignerAttribute(typeof(Designers.SQLiteConnectDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteConnectScope), new DesignerAttribute(typeof(Designers.SQLiteConnectScopeDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Schema.SQLiteCreateTable), new DesignerAttribute(typeof(Designers.SQLiteCreateTableDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteDisconnect), new DesignerAttribute(typeof(Designers.SQLiteDisconnectDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Write.SQLiteExecuteBatch), new DesignerAttribute(typeof(Designers.SQLiteExecuteBatchDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Write.SQLiteExecuteNonQuery), new DesignerAttribute(typeof(Designers.SQLiteExecuteNonQueryDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Query.SQLiteExecuteQuery), new DesignerAttribute(typeof(Designers.SQLiteExecuteQueryDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Query.SQLiteExecuteScalar), new DesignerAttribute(typeof(Designers.SQLiteExecuteScalarDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Write.SQLiteExecuteScript), new DesignerAttribute(typeof(Designers.SQLiteExecuteScriptDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Export.SQLiteExportToCsv), new DesignerAttribute(typeof(Designers.SQLiteExportToCsvDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Export.SQLiteExportToExcel), new DesignerAttribute(typeof(Designers.SQLiteExportToExcelDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Export.SQLiteExportToJson), new DesignerAttribute(typeof(Designers.SQLiteExportToJsonDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Schema.SQLiteGetTableNames), new DesignerAttribute(typeof(Designers.SQLiteGetTableNamesDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Schema.SQLiteGetTableSchema), new DesignerAttribute(typeof(Designers.SQLiteGetTableSchemaDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Export.SQLiteImportCsv), new DesignerAttribute(typeof(Designers.SQLiteImportCsvDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteMaintenance), new DesignerAttribute(typeof(Designers.SQLiteMaintenanceDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Query.SQLiteParallelQuery), new DesignerAttribute(typeof(Designers.SQLiteParallelQueryDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Maintenance.SQLiteSetPassword), new DesignerAttribute(typeof(Designers.SQLiteSetPasswordDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Schema.SQLiteTableExists), new DesignerAttribute(typeof(Designers.SQLiteTableExistsDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteTransactionScope), new DesignerAttribute(typeof(Designers.SQLiteTransactionScopeDesigner)));
            builder.AddCustomAttributes(typeof(Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteWriteLockScope), new DesignerAttribute(typeof(Designers.SQLiteWriteLockScopeDesigner)));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
