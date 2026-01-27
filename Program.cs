using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using PDFExtraction;

namespace GA_TroutStocking_Loader
{
    internal class Program
    {
        // Matches: "Weekly Trout Stocking Report: 12/15/2025 - 12/19/2025"
        private static readonly Regex ReportDatesRx =
            new(@"Weekly\s+Trout\s+Stocking\s+Report:\s*(?<dates>\d{1,2}/\d{1,2}/\d{4}\s*-\s*\d{1,2}/\d{1,2}/\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches data lines like:
        // 12/15/2025 Forsyth/Gwinnett Lanier Tailwater
        //private static readonly Regex RowRx =
        //    new(@"^(?<date>\d{1,2}/\d{1,2}/\d{4})\s+(?<county>.+?)\s+(?<waterbody>.+)$",
        //        RegexOptions.Compiled);
        private static readonly Regex RowRx =
            new(@"^(?<date>\d{1,2}/\d{1,2}/\d{4})(?<county>[A-Za-z]+(?:/[A-Za-z]+)*)(?<waterbody>.+)$",
                RegexOptions.Compiled);


        private const string InsertSql = @"
INSERT INTO dbo.WeeklyTroutStocking (ReportDates, StockingDate, County, WaterBody)
SELECT @ReportDates, @StockingDate, @County, @WaterBody
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.WeeklyTroutStocking
    WHERE StockingDate = @StockingDate
      AND County       = @County
      AND WaterBody    = @WaterBody
);";

        public static async Task Main()
        {
            // Minimal config loader (no extra packages)
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine($"Missing {configPath}");
                Environment.Exit(1);
            }

            var json = await File.ReadAllTextAsync(configPath);
            var pdfUrl = ReadJsonValue(json, "PdfUrl");
            var connStr = ReadJsonValue(json, "Sql", "ConnectionStrings");

            if (string.IsNullOrWhiteSpace(pdfUrl) || string.IsNullOrWhiteSpace(connStr))
            {
                Console.Error.WriteLine("appsettings.json is missing PdfUrl or ConnectionStrings:Sql");
                Environment.Exit(1);
            }

            // Code to extract stocking data from the PDF
            var tempPDFFilePath = await DownloadPdfAsync(pdfUrl, "");

            var (reportDates, rows) = WeeklyTroutStockingExtractor.Extract(tempPDFFilePath);

            if (reportDates == null)
            {
                Console.Error.WriteLine("Could not find 'Weekly Trout Stocking Report: ...' header in extracted text.");
                Environment.Exit(1);
            }


            Console.WriteLine($"ReportDates: {reportDates}");
            foreach (var r in rows)
            {
                Console.WriteLine($"{r.StockingDate} | {r.County} | {r.Waterbody}");
            }
            Console.WriteLine($"Parsed rows: {rows.Count}");

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            int inserted = 0;
            foreach (var r in rows)
            {
                var affected = await conn.ExecuteAsync(InsertSql, new
                {
                    ReportDates = reportDates,
                    StockingDate = r.StockingDate,
                    County = r.County,
                    Waterbody = r.Waterbody
                });

                if (affected > 0) inserted += affected;
            }

            Console.WriteLine($"Inserted new rows: {inserted}");
        }

        private static async Task<string> DownloadPdfAsync(string url, string bogus)
        {
            using var http = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            // Some endpoints behave better with a UA
            http.DefaultRequestHeaders.UserAgent.ParseAdd("GaTroutStockingLoader/1.0");

            var bytes = await http.GetByteArrayAsync(url);

            // Basic sanity check: PDF header "%PDF"
            if (bytes.Length < 4 || bytes[0] != (byte)'%' || bytes[1] != (byte)'P' || bytes[2] != (byte)'D' || bytes[3] != (byte)'F')
                throw new InvalidOperationException("Downloaded content does not appear to be a PDF.");

            string tempFile = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempFile, bytes);

            return tempFile;
        }

        // Very small JSON reader to avoid extra dependencies; supports:
        // ReadJsonValue(json, "PdfUrl") or ReadJsonValue(json, "Sql", "ConnectionStrings")
        private static string? ReadJsonValue(string json, string key, string? parentKey = null)
        {
            string pattern = parentKey == null
                ? $@"""{Regex.Escape(key)}""\s*:\s*""(?<v>[^""]+)"""
                : $@"""{Regex.Escape(parentKey)}""\s*:\s*\{{[\s\S]*?""{Regex.Escape(key)}""\s*:\s*""(?<v>[^""]+)""[\s\S]*?\}}";

            var m = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
            return m.Success ? m.Groups["v"].Value : null;
        }
    }

}