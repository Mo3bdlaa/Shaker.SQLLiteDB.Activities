#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteWriteLockScope"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class SQLiteWriteLockScopeViewModel : DesignPropertiesViewModel
    {
        public SQLiteWriteLockScopeViewModel(IDesignServices services)
            : base(services)
        {
        }

        public DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle> Connection { get; set; } = new DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle>();

        public DesignInArgument<string> DatabasePath { get; set; } = new DesignInArgument<string>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope> LockScope { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>();

        public DesignInArgument<string> LockFilePath { get; set; } = new DesignInArgument<string>();

        public DesignInArgument<int> LockTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignOutArgument<long> WaitedMilliseconds { get; set; } = new DesignOutArgument<long>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var order = 1;

            Connection.DisplayName = "Connection";
            Connection.Tooltip = "Connection whose database should be locked. Leave empty inside a SQLite Connect Scope.";
            Connection.Category = "Connection";
            Connection.IsPrincipal = true;
            Connection.OrderIndex = order++;
            Connection.Widget = new DefaultWidget { Type = "Input" };

            DatabasePath.DisplayName = "Database path";
            DatabasePath.Tooltip = "Database to lock, when the scope is used without a connection.";
            DatabasePath.Category = "Connection";
            DatabasePath.IsPrincipal = true;
            DatabasePath.OrderIndex = order++;
            DatabasePath.Widget = new DefaultWidget { Type = "Input" };

            LockScope.DisplayName = "Lock scope";
            LockScope.Tooltip = "Machine: a lock file serializes writers across processes and machines. Process: only inside this robot.";
            LockScope.Category = "Locking";
            LockScope.IsVisible = false;
            LockScope.OrderIndex = order++;
            LockScope.Widget = new DefaultWidget { Type = "Dropdown" };
            LockScope.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteLockScope>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            LockFilePath.DisplayName = "Lock file path";
            LockFilePath.Tooltip = "Lock file to use. Empty uses '<database file>.writelock'.";
            LockFilePath.Category = "Locking";
            LockFilePath.IsVisible = false;
            LockFilePath.OrderIndex = order++;
            LockFilePath.Widget = new DefaultWidget { Type = "Input" };

            LockTimeoutMilliseconds.DisplayName = "Lock timeout (ms)";
            LockTimeoutMilliseconds.Tooltip = "How long the scope waits for the lock before it fails.";
            LockTimeoutMilliseconds.Category = "Locking";
            LockTimeoutMilliseconds.IsVisible = false;
            LockTimeoutMilliseconds.OrderIndex = order++;
            LockTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            WaitedMilliseconds.DisplayName = "Waited (ms)";
            WaitedMilliseconds.Tooltip = "How long this scope had to wait before it got the lock.";
            WaitedMilliseconds.Category = "Output";
            WaitedMilliseconds.IsVisible = false;
            WaitedMilliseconds.OrderIndex = order++;
            WaitedMilliseconds.Widget = new DefaultWidget { Type = "Input" };
        }
    }
}
#endif
