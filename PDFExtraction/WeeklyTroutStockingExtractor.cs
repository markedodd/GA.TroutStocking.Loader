using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace PDFExtraction
{
    // This class is the main extraction logic for the Weekly Trout Stocking Report PDF.
    public static class WeeklyTroutStockingExtractor
    {
        // Header: "Weekly Trout Stocking Report: 12/15/2025 - 12/19/2025"
        private static readonly Regex ReportDatesRx =
            new(@"Weekly Trout Stocking Report:\s*(?<range>\d{1,2}/\d{1,2}/\d{4}\s*-\s*\d{1,2}/\d{1,2}/\d{4})",
                RegexOptions.Compiled);

        // Row example: "12/15/2025 Forsyth/Gwinnett Lanier Tailwater"
        // Uses 2+ spaces OR any whitespace run; then we later “best-effort” split into county + waterbody.
        private static readonly Regex RowRx =
            new(@"^(?<date>\d{1,2}/\d{1,2}/\d{4})\s+(?<rest>.+)$",
                RegexOptions.Compiled);

        public sealed record StockingRow(string StockingDate, string County, string Waterbody);

        public static (string reportDates, List<StockingRow> rows) Extract(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath)) throw new ArgumentException("pdfPath is required.");
            if (!File.Exists(pdfPath)) throw new FileNotFoundException("PDF not found.", pdfPath);

            using var doc = PdfDocument.Open(pdfPath);

            // The report is usually short; aggregate all pages in case GA adds pages later.
            var allLines = new List<string>();
            foreach (var page in doc.GetPages())
            {
                // More reliable than page.Text for “reading order” in many PDFs
                var text = ContentOrderTextExtractor.GetText(page);
                allLines.AddRange(SplitLines(text));
            }

            // 1) Extract report date range as a string
            // We search the entire text so we don’t depend on the line breaking.
            var fullText = string.Join("\n", allLines);
            var reportDates = ExtractReportDates(fullText);

            // 2) Find the start of the table, then parse each row line
            var rows = ExtractRowsFromLines(allLines);

            return (reportDates, rows);
        }

        public static async Task<(string reportDates, List<StockingRow> rows)> ExtractAsync(
            string pdfPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pdfPath)) throw new ArgumentException("pdfPath is required.");
            if (!File.Exists(pdfPath)) throw new FileNotFoundException("PDF not found.", pdfPath);

            await using var stream = new FileStream(
                pdfPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            // PdfPig itself is synchronous; this stream open is the async/I-O piece we can improve.
            // We still honor cancellation between pages.
            using var doc = PdfDocument.Open(stream);

            var allLines = new List<string>();
            foreach (var page in doc.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = ContentOrderTextExtractor.GetText(page);
                allLines.AddRange(SplitLines(text));
            }

            var fullText = string.Join("\n", allLines);
            var reportDates = ExtractReportDates(fullText);
            var rows = ExtractRowsFromLines(allLines);

            return (reportDates, rows);
        }

        private static string ExtractReportDates(string fullText)
        {
            var m = ReportDatesRx.Match(fullText);
            return m.Success ? m.Groups["range"].Value.Trim() : string.Empty;
        }

        private static List<StockingRow> ExtractRowsFromLines(List<string> lines)
        {
            // Identify where the table begins: look for header row containing DATE/COUNTY/WATERBODY
            var headerIndex = lines.FindIndex(l =>
                l.Contains("DATE", StringComparison.OrdinalIgnoreCase) &&
                l.Contains("COUNTY", StringComparison.OrdinalIgnoreCase) &&
                l.Contains("WATERBODY", StringComparison.OrdinalIgnoreCase));

            if (headerIndex < 0)
            {
                // If the header can’t be found, try parsing all lines anyway (safe fallback)
                headerIndex = -1;
            }

            var result = new List<StockingRow>();

            foreach (var raw in lines.Skip(headerIndex + 1))
            {
                var line = NormalizeWhitespace(raw);

                // Skip blank lines and obvious non-data lines
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("GEORGIA DEPARTMENT", StringComparison.OrdinalIgnoreCase)) continue;
                if (line.StartsWith("WILDLIFE RESOURCES", StringComparison.OrdinalIgnoreCase)) continue;

                var m = RowRx.Match(line);
                if (!m.Success) continue;

                var dateText = m.Groups["date"].Value.Trim();

                // Validate/normalize the date (optional but recommended)
                if (!TryParseMdy(dateText, out var dt)) continue;
                var stockingDate = dt.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

                var rest = m.Groups["rest"].Value.Trim();

                // Split the remainder into COUNTY and WATERBODY.
                // Best-effort strategy: county is usually one token group (sometimes contains "/"),
                // waterbody is the remainder. This matches the sample structure in your report.
                var (county, waterbody) = SplitCountyAndWaterbody(rest);

                // Each column as a string: StockingDate, County, Waterbody
                result.Add(new StockingRow(stockingDate, county, waterbody));
            }

            return result;
        }

        private static (string county, string waterbody) SplitCountyAndWaterbody(string rest)
        {
            // If the PDF text preserves multiple spaces between columns, prefer that.
            // Otherwise fall back to first “word” as county and remainder as waterbody.
            var partsByBigGaps = Regex.Split(rest, @"\s{2,}").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            if (partsByBigGaps.Length >= 2)
            {
                var county = partsByBigGaps[0].Trim();
                var waterbody = string.Join(" ", partsByBigGaps.Skip(1)).Trim();
                return (county, waterbody);
            }

            // Fallback: split on first whitespace
            var idx = rest.IndexOf(' ');
            if (idx <= 0) return (rest.Trim(), string.Empty);

            var county2 = rest.Substring(0, idx).Trim();
            var waterbody2 = rest.Substring(idx + 1).Trim();
            return (county2, waterbody2);
        }

        private static bool TryParseMdy(string s, out DateTime dt)
        {
            // Handle both M/d/yyyy and MM/dd/yyyy
            return DateTime.TryParseExact(s, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) ||
                   DateTime.TryParseExact(s, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
        }

        private static IEnumerable<string> SplitLines(string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Select(l => l.TrimEnd());
        }

        private static string NormalizeWhitespace(string s)
        {
            return Regex.Replace(s ?? string.Empty, @"\s+", " ").Trim();
        }
    }
}