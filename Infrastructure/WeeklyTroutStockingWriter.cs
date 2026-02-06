using Dapper;
using GA_TroutStocking_Loader.Services.Interfaces;
using Microsoft.Data.SqlClient;
using PDFExtraction;

namespace GA_TroutStocking_Loader.Infrastructure;

internal sealed class WeeklyTroutStockingWriter(IAppOptionsProvider optionsProvider) : IWeeklyTroutStockingWriter
{
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

    public async Task<int> InsertNewRowsAsync(
        string reportDates,
        IReadOnlyCollection<WeeklyTroutStockingExtractor.StockingRow> rows,
        CancellationToken cancellationToken = default)
    {
        var options = await optionsProvider.GetAsync(cancellationToken);

        using var conn = new SqlConnection(options.SqlConnectionString);
        await conn.OpenAsync(cancellationToken);
        Log.Logger.Info(LogJson.Message("SQL connection opened"));

        var inserted = 0;
        foreach (var r in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Log.Trace(LogJson.Message("Inserting row", new { r.StockingDate, r.County, r.Waterbody }));

            // Preserve existing parameter naming behavior.
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

        return inserted;
    }
}
