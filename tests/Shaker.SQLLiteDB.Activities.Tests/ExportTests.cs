using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Shaker.SQLLiteDB.Activities.IO;
using Xunit;

namespace Shaker.SQLLiteDB.Activities.Tests
{
    public class ExportTests
    {
        private static readonly CancellationToken None = CancellationToken.None;

        private static DataTable Sample()
        {
            var table = new DataTable("sample");
            table.Columns.Add("id", typeof(long));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("amount", typeof(double));
            table.Columns.Add("created", typeof(DateTime));
            table.Columns.Add("active", typeof(bool));

            table.Rows.Add(1L, "plain", 1.5, new DateTime(2024, 1, 2, 3, 4, 5), true);
            table.Rows.Add(2L, "has, comma", 2.5, new DateTime(2024, 6, 7), false);
            table.Rows.Add(3L, "has \"quotes\" and\nnewline", 3.5, DBNull.Value, DBNull.Value);

            return table;
        }

        [Fact]
        public void CsvQuotesOnlyWhatNeedsQuoting()
        {
            using var db = new TestDatabase();
            var path = db.File("out.csv");

            var rows = CsvExporter.Write(Sample(), path, new CsvFormat(), false, None);
            var text = File.ReadAllText(path);

            Assert.Equal(3, rows);
            Assert.Contains("id,name,amount,created,active", text);
            Assert.Contains("\"has, comma\"", text);
            Assert.Contains("\"has \"\"quotes\"\" and\nnewline\"", text);
            Assert.Contains("2024-01-02 03:04:05", text);
        }

        [Fact]
        public void CsvRoundTripKeepsEveryValue()
        {
            using var db = new TestDatabase();
            var path = db.File("roundtrip.csv");

            CsvExporter.Write(Sample(), path, new CsvFormat(), false, None);
            var back = CsvImporter.Read(path, new CsvFormat(), false, 0, None);

            Assert.Equal(3, back.Rows.Count);
            Assert.Equal(5, back.Columns.Count);
            Assert.Equal("has, comma", back.Rows[1]["name"]);
            Assert.Equal("has \"quotes\" and\nnewline", back.Rows[2]["name"]);
            Assert.Equal(string.Empty, back.Rows[2]["created"]);
        }

        [Fact]
        public void CsvSupportsOtherDelimitersAndEncodings()
        {
            using var db = new TestDatabase();
            var path = db.File("semicolon.csv");
            var format = new CsvFormat { Delimiter = ";", Encoding = new UTF8Encoding(false) };

            CsvExporter.Write(Sample(), path, format, false, None);
            var text = File.ReadAllText(path);
            Assert.Contains("id;name;amount", text);

            var back = CsvImporter.Read(path, format, false, 0, None);
            Assert.Equal("has, comma", back.Rows[1]["name"]);
        }

        [Fact]
        public void CsvHandlesAMultiCharacterDelimiter()
        {
            using var db = new TestDatabase();
            var path = db.File("pipes.csv");
            var format = new CsvFormat { Delimiter = "||" };

            CsvExporter.Write(Sample(), path, format, false, None);
            var back = CsvImporter.Read(path, format, false, 0, None);

            Assert.Equal(5, back.Columns.Count);
            Assert.Equal("plain", back.Rows[0]["name"]);
        }

        [Fact]
        public void CsvAppendDoesNotRepeatTheHeader()
        {
            using var db = new TestDatabase();
            var path = db.File("append.csv");

            CsvExporter.Write(Sample(), path, new CsvFormat(), false, None);
            CsvExporter.Write(Sample(), path, new CsvFormat(), true, None);

            var lines = File.ReadAllLines(path);
            Assert.Equal(1, lines.Count(line => line.StartsWith("id,name,amount", StringComparison.Ordinal)));
        }

        [Fact]
        public void CsvImportCanDetectColumnTypes()
        {
            using var db = new TestDatabase();
            var path = db.File("typed.csv");
            File.WriteAllText(path, "id,name,amount,when,flag\r\n1,Sara,2.5,2024-03-04,True\r\n2,Omar,3.5,2024-03-05,False\r\n");

            var table = CsvImporter.Read(path, new CsvFormat(), true, 0, None);

            Assert.Equal(typeof(long), table.Columns["id"].DataType);
            Assert.Equal(typeof(string), table.Columns["name"].DataType);
            Assert.Equal(typeof(double), table.Columns["amount"].DataType);
            Assert.Equal(typeof(DateTime), table.Columns["when"].DataType);
            Assert.Equal(typeof(bool), table.Columns["flag"].DataType);
            Assert.Equal(2, table.Rows.Count);
        }

        [Fact]
        public void XlsxIsAValidWorkbookWithTypedCells()
        {
            using var db = new TestDatabase();
            var path = db.File("book.xlsx");

            var rows = XlsxWriter.Write(path, "People", Sample(), new XlsxOptions(), None);
            Assert.Equal(3, rows);

            using var archive = ZipFile.OpenRead(path);
            var names = archive.Entries.Select(entry => entry.FullName).ToList();

            Assert.Contains("[Content_Types].xml", names);
            Assert.Contains("_rels/.rels", names);
            Assert.Contains("xl/workbook.xml", names);
            Assert.Contains("xl/_rels/workbook.xml.rels", names);
            Assert.Contains("xl/styles.xml", names);
            Assert.Contains("xl/worksheets/sheet1.xml", names);

            var workbook = ReadXml(archive, "xl/workbook.xml");
            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var sheet = workbook.Descendants(main + "sheet").Single();
            Assert.Equal("People", sheet.Attribute("name").Value);

            var worksheet = ReadXml(archive, "xl/worksheets/sheet1.xml");
            var cells = worksheet.Descendants(main + "c").ToList();

            var headerCell = cells.First(c => c.Attribute("r").Value == "A1");
            Assert.Equal("id", headerCell.Descendants(main + "t").Single().Value);
            Assert.Equal("1", headerCell.Attribute("s").Value);

            var numberCell = cells.First(c => c.Attribute("r").Value == "A2");
            Assert.Null(numberCell.Attribute("t"));
            Assert.Equal("1", numberCell.Element(main + "v").Value);

            var dateCell = cells.First(c => c.Attribute("r").Value == "D2");
            Assert.Equal("2", dateCell.Attribute("s").Value);
            Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5).ToOADate(),
                double.Parse(dateCell.Element(main + "v").Value, System.Globalization.CultureInfo.InvariantCulture), 6);

            var boolCell = cells.First(c => c.Attribute("r").Value == "E2");
            Assert.Equal("b", boolCell.Attribute("t").Value);
            Assert.Equal("1", boolCell.Element(main + "v").Value);

            // A NULL is written as no cell at all, which is how Excel represents an empty cell.
            Assert.DoesNotContain(cells, c => c.Attribute("r").Value == "D4");

            Assert.Single(worksheet.Descendants(main + "autoFilter"));
            Assert.Single(worksheet.Descendants(main + "pane"));
        }

        [Fact]
        public void XlsxWritesOneSheetPerQuery()
        {
            using var db = new TestDatabase();
            var path = db.File("multi.xlsx");

            var sheets = new[] { new XlsxSheet("First", Sample()), new XlsxSheet("Second", Sample()) };
            var rows = XlsxWriter.Write(path, sheets, new XlsxOptions(), None);

            Assert.Equal(6, rows);

            using var archive = ZipFile.OpenRead(path);
            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var names = ReadXml(archive, "xl/workbook.xml").Descendants(main + "sheet")
                .Select(sheet => sheet.Attribute("name").Value).ToList();

            Assert.Equal(new[] { "First", "Second" }, names);
            Assert.Contains("xl/worksheets/sheet2.xml", archive.Entries.Select(entry => entry.FullName));
        }

        [Fact]
        public void SheetNamesAreMadeLegalAndUnique()
        {
            var used = new System.Collections.Generic.List<string>();
            Assert.Equal("a_b_c", XlsxWriter.SanitizeSheetName("a[b]c", used));
            used.Add("a_b_c");
            Assert.Equal("a_b_c_2", XlsxWriter.SanitizeSheetName("a[b]c", used));
            Assert.Equal(31, XlsxWriter.SanitizeSheetName(new string('x', 60), null).Length);
        }

        [Fact]
        public void ColumnLettersFollowTheExcelScheme()
        {
            Assert.Equal("A", XlsxWriter.ColumnName(0));
            Assert.Equal("Z", XlsxWriter.ColumnName(25));
            Assert.Equal("AA", XlsxWriter.ColumnName(26));
            Assert.Equal("AZ", XlsxWriter.ColumnName(51));
            Assert.Equal("BA", XlsxWriter.ColumnName(52));
            Assert.Equal("XFD", XlsxWriter.ColumnName(16383));
        }

        [Fact]
        public void ControlCharactersAreStrippedSoExcelCanOpenTheFile()
        {
            using var db = new TestDatabase();
            var path = db.File("control.xlsx");

            var table = new DataTable("t");
            table.Columns.Add("v", typeof(string));
            table.Rows.Add("before\u0001after");

            XlsxWriter.Write(path, "Sheet1", table, new XlsxOptions(), None);

            using var archive = ZipFile.OpenRead(path);
            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var value = ReadXml(archive, "xl/worksheets/sheet1.xml").Descendants(main + "t").Last().Value;
            Assert.Equal("beforeafter", value);
        }

        [Fact]
        public void JsonUsesRealTypesAndEscapesText()
        {
            var json = JsonExporter.ToJson(Sample(), false, null);

            Assert.StartsWith("[", json);
            Assert.Contains("\"id\":1", json);
            Assert.Contains("\"amount\":1.5", json);
            Assert.Contains("\"active\":true", json);
            Assert.Contains("\"created\":null", json);
            Assert.Contains("\\\"quotes\\\"", json);
            Assert.Contains("\\n", json);
        }

        [Fact]
        public void JsonCanBeWrittenToAFile()
        {
            using var db = new TestDatabase();
            var path = db.File("out.json");

            var rows = JsonExporter.Write(Sample(), path, true, null, null, None);

            Assert.Equal(3, rows);
            var text = File.ReadAllText(path);
            Assert.Contains("\"name\": \"plain\"", text);
        }

        private static XDocument ReadXml(ZipArchive archive, string entryName)
        {
            using var stream = archive.GetEntry(entryName).Open();
            return XDocument.Load(stream);
        }
    }
}
