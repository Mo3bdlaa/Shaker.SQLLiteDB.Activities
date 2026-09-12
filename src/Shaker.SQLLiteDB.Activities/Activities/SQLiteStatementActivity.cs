using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>Base class of the activities that run a SQL statement with parameters.</summary>
    public abstract class SQLiteStatementActivity<TResult> : SQLiteActivityBase<TResult>
    {
        /// <summary>The SQL statement to run.</summary>
        [Category("Input")]
        [DisplayName("SQL")]
        [Description("The SQL statement. Use named parameters (@name) or question marks, never string concatenation.")]
        [RequiredArgument]
        public InArgument<string> Sql { get; set; }

        /// <summary>Named parameters, for example <c>{"@id", 42}</c>. The @ is optional.</summary>
        [Category("Input")]
        [DisplayName("Parameters")]
        [Description("Named parameters as a Dictionary(Of String, Object). The leading @ is optional. Keeps the query safe from SQL injection.")]
        public InArgument<IDictionary<string, object>> Parameters { get; set; }

        /// <summary>Values for the <c>?</c> placeholders, in order.</summary>
        [Category("Input")]
        [DisplayName("Parameter values")]
        [Description("Values for the '?' placeholders of the statement, in order. An alternative to named parameters.")]
        public InArgument<IEnumerable<object>> ParameterValues { get; set; }
    }
}
