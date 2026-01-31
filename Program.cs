using Dapper;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.RegularExpressions;
using PDFExtraction;

namespace GA_TroutStocking_Loader
{
    public class Program
    {
        private static readonly Regex ReportDatesRx =
            new(@"Weekly\s+Trout\s+Stocking\s+Report:\s*(?<dates>\d{1,2}/\d{1,2}/\d{4}\s*-\s*\d{1,2}/\d{1,2}/\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            Log.Initialize(AppContext.BaseDirectory);

            Log.Logger.Info(LogJson.Message("Application starting"));

            try
            {
                var config = await LoadConfigAsync();

                Log.Logger.Info(LogJson.Message("Configuration loaded", new { config.PdfUrl }));

                Log.Logger.Info(LogJson.Message("Downloading PDF"));
                var tempPDFFilePath = await DownloadPdfAsync(config.PdfUrl);

                Log.Logger.Debug(LogJson.Message("PDF downloaded", new { tempPDFFilePath }));

                Log.Logger.Info(LogJson.Message("Extracting PDF rows"));
                var (reportDates, rows) = await WeeklyTroutStockingExtractor.ExtractAsync(tempPDFFilePath);

                if (string.IsNullOrWhiteSpace(reportDates))
                {
                    Log.Logger.Error(LogJson.Message("Could not find report header date range"));
                    Console.Error.WriteLine("Could not find 'Weekly Trout Stocking Report: ...' header in extracted text.");
                    Environment.Exit(1);
                    return;
                }

                Log.Logger.Info(LogJson.Message("Extraction complete", new { reportDates, rowCount = rows.Count }));

                using var conn = new SqlConnection(config.SqlConnectionString);
                await conn.OpenAsync();

                Log.Logger.Info(LogJson.Message("SQL connection opened"));

                var inserted = 0;
                foreach (var r in rows)
                {
                    Log.Trace(LogJson.Message("Inserting row", new { r.StockingDate, r.County, r.Waterbody }));

                    var affected = await conn.ExecuteAsync(InsertSql, new
                    {
                        ReportDates = reportDates,
                        StockingDate = r.StockingDate,
                        County = r.County,
                        Waterbody = r.Waterbody
                    });

                    if (affected > 0)
                    {
                        inserted += affected;
                    }
                }

                Log.Logger.Info(LogJson.Message("Insert complete", new { inserted }));

                Console.WriteLine($"Inserted new rows: {inserted}");

                Log.Logger.Info(LogJson.Message("Application completed successfully"));
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(LogJson.Message("Application failed"), ex);
                Console.Error.WriteLine(ex.ToString());
                Environment.ExitCode = 1;
            }
        }

        private static async Task<AppConfig> LoadConfigAsync()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
            {
                Log.Logger.Error(LogJson.Message("Missing configuration file", new { configPath }));
                Console.Error.WriteLine($"Missing {configPath}");
                Environment.Exit(1);
            }

            var json = await File.ReadAllTextAsync(configPath);

            var pdfUrl = ReadJsonValue(json, "PdfUrl");
            var connStr = ReadJsonValue(json, "Sql", "ConnectionStrings");

            if (string.IsNullOrWhiteSpace(pdfUrl) || string.IsNullOrWhiteSpace(connStr))
            {
                Log.Logger.Error(LogJson.Message("Configuration missing required keys", new
                {
                    hasPdfUrl = !string.IsNullOrWhiteSpace(pdfUrl),
                    hasSql = !string.IsNullOrWhiteSpace(connStr)
                }));

                Console.Error.WriteLine("appsettings.json is missing PdfUrl or ConnectionStrings:Sql");
                Environment.Exit(1);
            }

            return new AppConfig(pdfUrl!, connStr!);
        }

        private static async Task<string> DownloadPdfAsync(string url)
        {
            try
            {
                using var http = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                });

                http.DefaultRequestHeaders.UserAgent.ParseAdd("GaTroutStockingLoader/1.0");

                var bytes = await http.GetByteArrayAsync(url);

                if (bytes.Length < 4 || bytes[0] != (byte)'%' || bytes[1] != (byte)'P' || bytes[2] != (byte)'D' || bytes[3] != (byte)'F')
                {
                    throw new InvalidOperationException("Downloaded content does not appear to be a PDF.");
                }

                var tempFile = Path.GetTempFileName();
                await File.WriteAllBytesAsync(tempFile, bytes);

                return tempFile;
            }
            catch (Exception ex)
            {
                log4net.ThreadContext.Properties["jsonException"] = LogJson.Exception(ex);
                Log.Logger.Error(LogJson.Message("Failed to download PDF", new { url }), ex);
                log4net.ThreadContext.Properties.Remove("jsonException");

                throw;
            }
        }

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