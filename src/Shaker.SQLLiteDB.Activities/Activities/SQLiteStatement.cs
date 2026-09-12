using System.Collections.Generic;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>One statement of a batch: the SQL plus its parameters.</summary>
    public class SQLiteStatement
    {
        public SQLiteStatement()
        {
        }

        public SQLiteStatement(string sql)
        {
            Sql = sql;
        }

        public SQLiteStatement(string sql, IDictionary<string, object> parameters)
        {
            Sql = sql;
            Parameters = parameters;
        }

        /// <summary>The statement to run.</summary>
        public string Sql { get; set; }

        /// <summary>Named parameters of the statement. The leading @ is optional.</summary>
        public IDictionary<string, object> Parameters { get; set; }

        public override string ToString()
        {
            return Sql;
        }
    }
}
