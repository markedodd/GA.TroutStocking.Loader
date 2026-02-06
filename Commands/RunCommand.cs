using GA_TroutStocking_Loader.Services.Interfaces;
using PDFExtraction;

namespace GA_TroutStocking_Loader.Commands;

/// <summary>
/// Orchestrates the application workflow. Intentionally preserves existing behavior/output.
/// </summary>
internal sealed class RunCommand(
    IAppOptionsProvider optionsProvider,
    IPdfDownloader pdfDownloader,
    IWeeklyTroutStockingExtractor extractor,
    IWeeklyTroutStockingWriter writer,
    IEnvironmentExit environmentExit,
    IConsole console) : IAppCommand
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Log.Logger.Info(LogJson.Message("Application starting"));

        try
        {
            var options = await optionsProvider.GetAsync(cancellationToken);
            Log.Logger.Info(LogJson.Message("Configuration loaded", new { options.PdfUrl }));

            Log.Logger.Info(LogJson.Message("Downloading PDF"));
            var tempPDFFilePath = await pdfDownloader.DownloadAsync(options.PdfUrl, cancellationToken);
            Log.Logger.Debug(LogJson.Message("PDF downloaded", new { tempPDFFilePath }));

            Log.Logger.Info(LogJson.Message("Extracting PDF rows"));
            var (reportDates, rows) = await extractor.ExtractAsync(tempPDFFilePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(reportDates))
            {
                Log.Logger.Error(LogJson.Message("Could not find report header date range"));
                console.WriteErrorLine("Could not find 'Weekly Trout Stocking Report: ...' header in extracted text.");
                environmentExit.Exit(1);
                return;
            }
            Log.Logger.Info(LogJson.Message("Extraction complete", new { reportDates, rowCount = rows.Count }));

            var inserted = await writer.InsertNewRowsAsync(reportDates, rows, cancellationToken);

            console.WriteLine($"Inserted new rows: {inserted}");

            Log.Logger.Info(LogJson.Message("Application completed successfully"));
        }
        catch (Exception ex)
        {
            Log.Logger.Fatal(LogJson.Message("Application failed"), ex);
            console.WriteErrorLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }
}
