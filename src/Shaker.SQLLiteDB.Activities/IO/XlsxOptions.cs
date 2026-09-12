using System;

namespace Shaker.SQLLiteDB.Activities.IO
{
    /// <summary>Layout options for the XLSX export.</summary>
    public class XlsxOptions
    {
        /// <summary>Write the column names as the first row, in bold.</summary>
        public bool IncludeHeaders { get; set; } = true;

        /// <summary>Freeze the header row so that it stays visible while scrolling.</summary>
        public bool FreezeHeaderRow { get; set; } = true;

        /// <summary>Add Excel's auto filter dropdowns to the header row.</summary>
        public bool AutoFilter { get; set; } = true;

        /// <summary>Give every column a width derived from its content.</summary>
        public bool AutoSizeColumns { get; set; } = true;

        /// <summary>Number format used for date and time values.</summary>
        public string DateTimeFormat { get; set; } = "yyyy\\-mm\\-dd hh:mm:ss";

        /// <summary>Number format used for values that carry no time part.</summary>
        public string DateFormat { get; set; } = "yyyy\\-mm\\-dd";

        /// <summary>
        /// Split a result that exceeds the Excel row limit over several sheets instead of failing.
        /// The extra sheets are named <c>Sheet (2)</c>, <c>Sheet (3)</c> and so on.
        /// </summary>
        public bool SplitLargeTables { get; set; } = true;

        /// <summary>Maximum number of rows Excel accepts in one worksheet.</summary>
        public const int ExcelMaxRowsPerSheet = 1048576;

        /// <summary>Maximum number of columns Excel accepts in one worksheet.</summary>
        public const int ExcelMaxColumns = 16384;
    }
}
