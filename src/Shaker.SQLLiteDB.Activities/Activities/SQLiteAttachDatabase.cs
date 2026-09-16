using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using Shaker.SQLLiteDB.Activities.Core;
using Shaker.SQLLiteDB.Activities.Activities;

namespace Shaker.SQLLiteDB.Activities.Activities.Maintenance
{
    /// <summary>
    /// Attaches a second database file to the current connection, so one query can join tables from
    /// both files, for example <c>select * from main.orders join archive.orders_2023 …</c>.
    /// </summary>
    [Category("SQLite.Advanced")]
    [DisplayName("SQLite Attach Database")]
    [Description("Attaches another database file to the connection under an alias, so queries can span both files.")]
    public class SQLiteAttachDatabase : SQLiteActivityBase<bool>
    {
        public SQLiteAttachDatabase()
        {
            DisplayName = "SQLite Attach Database";
        }

        [Category("Input")]
        [DisplayName("Database to attach")]
        [Description("Path of the database file to attach.")]
        [RequiredArgument]
        public InArgument<string> AttachPath { get; set; }

        [Category("Input")]
        [DisplayName("Alias")]
        [Description("Name the attached database gets in SQL, for example 'archive'.")]
        [RequiredArgument]
        public InArgument<string> Alias { get; set; }

        [Category("Options")]
        [DisplayName("Detach instead")]
        [Description("Detach the alias again instead of attaching it.")]
        public bool Detach { get; set; }

        protected override SQLiteRun CreateRun(NativeActivityContext context)
        {
            var request = ResolveConnection(context);
            var path = GetValue(AttachPath, context, null);
            var alias = Alias.Get(context);
            var detach = Detach;

            return new SQLiteRun
            {
                Work = cancellationToken =>
                {
                    using (var lease = request.Acquire())
                    {
                        var sql = detach
                            ? "DETACH DATABASE " + SQLiteSchema.Quote(alias) + ";"
                            : "ATTACH DATABASE @path AS " + SQLiteSchema.Quote(alias) + ";";

                        var parameters = detach ? null : new Dictionary<string, object> { { "@path", path } };

                        lease.Handle.Execute((connection, transaction) =>
                        {
                            using (var command = SQLiteCommandExecutor.CreateCommand(connection, transaction, sql, parameters, null, lease.Handle.Settings.CommandTimeoutSeconds))
                            {
                                return command.ExecuteNonQuery();
                            }
                        }, cancellationToken);

                        return true;
                    }
                }
            };
        }
    }
}
