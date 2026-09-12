using System;
using System.Text;

namespace Shaker.SQLLiteDB.Activities.IO
{
    /// <summary>Formatting options shared by the CSV export and import activities.</summary>
    public class CsvFormat
    {
        /// <summary>Field separator. Default is a comma.</summary>
        public string Delimiter { get; set; } = ",";

        /// <summary>Character used to wrap a field that contains a delimiter, a quote or a line break.</summary>
        public char QuoteCharacter { get; set; } = '"';

        /// <summary>Write the column names as the first line.</summary>
        public bool IncludeHeaders { get; set; } = true;

        /// <summary>Wrap every field in quotes, not only the ones that need it.</summary>
        public bool QuoteAllFields { get; set; }

        /// <summary>File encoding. Default is UTF-8 with a byte order mark so that Excel opens it correctly.</summary>
        public Encoding Encoding { get; set; } = new UTF8Encoding(true);

        /// <summary>Line ending written between records.</summary>
        public string NewLine { get; set; } = "\r\n";

        /// <summary>Format applied to <see cref="DateTime"/> values. Default is a round trippable, sortable format.</summary>
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary>Text written for a NULL value.</summary>
        public string NullText { get; set; } = "";

        /// <summary>Culture used to format numbers and dates. Default is the invariant culture.</summary>
        public string CultureName { get; set; }

        public System.Globalization.CultureInfo GetCulture()
        {
            if (string.IsNullOrWhiteSpace(CultureName))
            {
                return System.Globalization.CultureInfo.InvariantCulture;
            }

            try
            {
                return System.Globalization.CultureInfo.GetCultureInfo(CultureName);
            }
            catch (System.Globalization.CultureNotFoundException)
            {
                return System.Globalization.CultureInfo.InvariantCulture;
            }
        }
    }
}
