using PDFExtraction;

namespace GA_TroutStocking_Loader.Services.Interfaces;

internal interface IWeeklyTroutStockingWriter
{
    Task<int> InsertNewRowsAsync(
        string reportDates,
        IReadOnlyCollection<WeeklyTroutStockingExtractor.StockingRow> rows,
        CancellationToken cancellationToken = default);
}
