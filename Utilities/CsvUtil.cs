using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Oecd.Utilities
{
    public static class CsvUtil
    {
        public static string EscapeQuotes(this string self)
        {
            return self?.Replace("\"", "\"\"") ?? "";
        }

        public static string Surround(this string self, string before, string after)
        {
            return $"{before}{self}{after}";
        }

        public static string Quoted(this string self, string quotes = "\"")
        {
            return self.Surround(quotes, quotes);
        }

        public static string QuotedCSVFieldIfNecessary(this string self)
        {
            return (self == null) ? "" : self.Contains('"') ? self.Quoted() : self;
        }

        public static string ToCsvField(this string self)
        {
            return self.EscapeQuotes().QuotedCSVFieldIfNecessary();
        }

        public static string ToCsvRow(this IEnumerable<string> self)
        {
            return string.Join(",", self.Select(ToCsvField));
        }

        public static IEnumerable<string> ToCsvRows(this DataTable self)
        {
            yield return self.Columns.OfType<object>().Select(c => c.ToString()).ToCsvRow();
            foreach (var dr in self.Rows.OfType<DataRow>())
                yield return dr.ItemArray.Select(item => item.ToString()).ToCsvRow();
        }

        public static void ToCsvFile(this DataTable self, string path, System.Text.Encoding encoding)
        {
            File.WriteAllLines(path, self.ToCsvRows(), encoding);
        }
    }
}
