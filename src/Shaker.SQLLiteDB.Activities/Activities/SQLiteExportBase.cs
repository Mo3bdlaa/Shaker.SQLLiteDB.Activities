using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using Shaker.SQLLiteDB.Activities.Core;

namespace Shaker.SQLLiteDB.Activities.Activities
{
    /// <summary>
    /// Base class of the export activities. The data comes either from a query that this activity runs,
    /// or from a DataTable the workflow already has.
    /// </summary>
    public abstract class SQLiteExportBase<TResult> : SQLiteActivityBase<TResult>
    {
        [Category("Input")]
        [DisplayName("SQL")]
        [Description("Query whose result is exported. Leave empty when 'Data table' is supplied.")]
        public InArgument<string> Sql { get; set; }

        [Category("Input")]
        [DisplayName("Parameters")]
        [Description("Named parameters of the query, as a Dictionary(Of String, Object).")]
        public InArgument<IDictionary<string, object>> Parameters { get; set; }

        [Category("Input")]
        [DisplayName("Table to export")]
        [Description("Name of a table to export completely. A simpler alternative to writing a SELECT.")]
        public InArgument<string> SourceTableName { get; set; }

        [Category("Input")]
        [DisplayName("Data table")]
        [Description("Rows to export. When this is supplied, no query is run and no connection is needed.")]
        public InArgument<DataTable> DataTable { get; set; }

        /// <summary>Builds the statement to export, from either 'Sql' or 'Table to export'.</summary>
        protected string ResolveQuery(NativeActivityContext context)
        {
            var sql = GetValue(Sql, context, null);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            var table = GetValue(SourceTableName, context, null);
            if (!string.IsNullOrWhiteSpace(table))
            {
                return "select * from " + SQLiteSchema.QualifiedName(table) + ";";
            }

            return null;
        }

        /// <summary>Resolves the text encoding from its name, falling back to UTF-8 with a byte order mark.</summary>
        protected static Encoding ResolveEncoding(string encodingName)
        {
            if (string.IsNullOrWhiteSpace(encodingName))
            {
                return new UTF8Encoding(true);
            }

            try
            {
                return Encoding.GetEncoding(encodingName.Trim());
            }
            catch (System.ArgumentException)
            {
                throw new SQLiteActivityException(
                    "Unknown encoding '" + encodingName + "'. Use for example utf-8, utf-16, windows-1256 or iso-8859-1.");
            }
        }
    }
}
