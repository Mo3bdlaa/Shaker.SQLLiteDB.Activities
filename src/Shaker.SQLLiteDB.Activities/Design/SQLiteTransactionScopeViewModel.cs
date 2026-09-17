#if NET6_0_OR_GREATER
using System.Activities.DesignViewModels;

namespace Shaker.SQLLiteDB.Activities.Design
{
    /// <summary>
    /// Designer layout for <see cref="Shaker.SQLLiteDB.Activities.Activities.Connection.SQLiteTransactionScope"/>: which fields are drawn inside the
    /// activity itself, in what order, and how each one is edited. Generated - see tools/generate-viewmodels.
    /// </summary>
    public class SQLiteTransactionScopeViewModel : DesignPropertiesViewModel
    {
        public SQLiteTransactionScopeViewModel(IDesignServices services)
            : base(services)
        {
        }

        public DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle> Connection { get; set; } = new DesignInArgument<Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle>();

        public DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteTransactionMode> TransactionMode { get; set; } = new DesignProperty<Shaker.SQLLiteDB.Activities.Core.SQLiteTransactionMode>();

        public DesignProperty<bool> TakeWriteLock { get; set; } = new DesignProperty<bool>();

        public DesignInArgument<int> LockTimeoutMilliseconds { get; set; } = new DesignInArgument<int>();

        public DesignOutArgument<bool> Committed { get; set; } = new DesignOutArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var order = 1;

            Connection.DisplayName = "Connection";
            Connection.Tooltip = "Connection to work on. Leave empty when the scope sits inside a SQLite Connect Scope.";
            Connection.Category = "Connection";
            Connection.IsPrincipal = true;
            Connection.OrderIndex = order++;
            Connection.Widget = new DefaultWidget { Type = "Input" };

            TransactionMode.DisplayName = "Transaction mode";
            TransactionMode.Tooltip = "Immediate takes the write lock right away and is the safer choice when several robots write. Deferred waits for the first write.";
            TransactionMode.Category = "Transaction";
            TransactionMode.IsPrincipal = true;
            TransactionMode.OrderIndex = order++;
            TransactionMode.Widget = new DefaultWidget { Type = "Dropdown" };
            TransactionMode.DataSource = EnumDataSourceBuilder<Shaker.SQLLiteDB.Activities.Core.SQLiteTransactionMode>.Build(DataSourceEnumOrder.OrderById, value => value.ToString());

            TakeWriteLock.DisplayName = "Take writer lock";
            TakeWriteLock.Tooltip = "Hold the writer lock for the whole transaction so that other processes queue up instead of failing with 'database is locked'.";
            TakeWriteLock.Category = "Transaction";
            TakeWriteLock.IsVisible = false;
            TakeWriteLock.OrderIndex = order++;
            TakeWriteLock.Widget = new DefaultWidget { Type = "Checkbox" };

            LockTimeoutMilliseconds.DisplayName = "Lock timeout (ms)";
            LockTimeoutMilliseconds.Tooltip = "How long the scope waits for the writer lock before it fails.";
            LockTimeoutMilliseconds.Category = "Transaction";
            LockTimeoutMilliseconds.IsVisible = false;
            LockTimeoutMilliseconds.OrderIndex = order++;
            LockTimeoutMilliseconds.Widget = new DefaultWidget { Type = "Input" };

            Committed.DisplayName = "Committed";
            Committed.Tooltip = "True when the transaction was committed, false when it was rolled back.";
            Committed.Category = "Output";
            Committed.IsVisible = false;
            Committed.OrderIndex = order++;
            Committed.Widget = new DefaultWidget { Type = "Checkbox" };
        }
    }
}
#endif
