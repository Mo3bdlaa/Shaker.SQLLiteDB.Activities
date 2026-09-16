using System.Activities;
using System.ComponentModel;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Connection
{
    /// <summary>
    /// Closes a connection that was opened without a scope. A SQLite Connect Scope closes its own
    /// connection, so this activity is only needed when the connection is passed around by hand.
    /// </summary>
    [Category("SQLite.Connection")]
    [DisplayName("SQLite Disconnect")]
    [Description("Closes a SQLite connection that was opened outside a scope.")]
#if NET6_0_OR_GREATER
    [System.Activities.ViewModels.ViewModelClass(typeof(Shaker.SQLLiteDB.Activities.Design.SQLiteDisconnectViewModel))]
#endif
    public class SQLiteDisconnect : CodeActivity<bool>
    {
        public SQLiteDisconnect()
        {
            DisplayName = "SQLite Disconnect";
        }

        [Category("Input")]
        [DisplayName("Connection")]
        [Description("The connection to close. Closing an already closed connection does nothing.")]
        [RequiredArgument]
        public InArgument<SQLiteConnectionHandle> Connection { get; set; }

        protected override bool Execute(CodeActivityContext context)
        {
            var handle = Connection.Get(context);
            if (handle == null)
            {
                return false;
            }

            var wasOpen = handle.IsOpen;
            handle.Dispose();
            return wasOpen;
        }
    }
}
