using GA_TroutStocking_Loader.Services.Interfaces;
using PDFExtraction;

namespace GA_TroutStocking_Loader.Infrastructure;

internal sealed class WeeklyTroutStockingExtractorAdapter : IWeeklyTroutStockingExtractor
{
    public Task<(string reportDates, List<WeeklyTroutStockingExtractor.StockingRow> rows)> ExtractAsync(
        string pdfPath,
        CancellationToken cancellationToken = default)
        => WeeklyTroutStockingExtractor.ExtractAsync(pdfPath, cancellationToken);
}
