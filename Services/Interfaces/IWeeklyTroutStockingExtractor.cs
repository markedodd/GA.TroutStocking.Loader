using PDFExtraction;

namespace GA_TroutStocking_Loader.Services.Interfaces;

internal interface IWeeklyTroutStockingExtractor
{
    Task<(string reportDates, List<WeeklyTroutStockingExtractor.StockingRow> rows)> ExtractAsync(
        string pdfPath,
        CancellationToken cancellationToken = default);
}
