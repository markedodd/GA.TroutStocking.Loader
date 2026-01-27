# GA.TroutStocking.Loader

A small .NET 8 console app that downloads the Georgia DNR **Weekly Trout Stocking Report** PDF, extracts the stocking rows (date / county / waterbody), and inserts new records into SQL Server.

## What it does

- Downloads the Weekly Trout Stocking Report PDF from a configured URL.
- Extracts:
  - the report date range (e.g. `12/15/2025 - 12/19/2025`)
  - stocking rows: `StockingDate`, `County`, `Waterbody`
- Inserts rows into `dbo.WeeklyTroutStocking` using Dapper.
- De-duplicates inserts using a `WHERE NOT EXISTS` check on `(StockingDate, County, WaterBody)`.

## Tech

- .NET 8
- C# 12
- [Dapper](https://github.com/DapperLib/Dapper)
- [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)
- [UglyToad.PdfPig](https://github.com/UglyToad/PdfPig)

## Project structure

- `Program.cs`: app entry point, configuration, download, and SQL insert logic.
- `WeeklyTroutStockingExtractor/WeeklyTroutStockingExtractor.cs`: PDF parsing/extraction logic (namespace `WeeklyTroutStockingExtractor`).

## Prerequisites

- .NET 8 SDK
- SQL Server (local or remote)
- A database containing table `dbo.WeeklyTroutStocking`

## Database

### Expected table schema

This project assumes a table with (at minimum) the following columns:

- `ReportDates` (string / nvarchar)
- `StockingDate` (date or string / nvarchar)
- `County` (string / nvarchar)
- `WaterBody` (string / nvarchar)

> Note: the insert statement uses parameters `@ReportDates`, `@StockingDate`, `@County`, and `@WaterBody`. Ensure the parameter names supplied by the app match these names.

## Configuration

1. Copy `appsettings.example.json` to `appsettings.json`.
2. Update the values for the environment.

> Important: `appsettings.json` should not be committed to source control if it contains secrets (server names, credentials, etc.).

The app loads `appsettings.json` from `AppContext.BaseDirectory` (typically `bin\Debug\net8.0\` / `bin\Release\net8.0\`), so ensure the file is present alongside the built executable.

Example `appsettings.json`:

```json
{
  "PdfUrl": "https://example.com/weekly_trout_stocking_report.pdf",
  "ConnectionStrings": {
    "Sql": "Server=.;Database=MyDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Keys

- `PdfUrl`: URL to the Weekly Trout Stocking Report PDF.
- `ConnectionStrings:Sql`: SQL Server connection string.

## Run

From Visual Studio:

- Set the console project as the startup project.
- Run with __Debug > Start Without Debugging__.

From the command line:

```shell
dotnet run --project .\GA_TroutStocking_Loader\GA_TroutStocking_Loader.csproj
```

The app writes parsed rows and an insert count to stdout.

## Notes / Troubleshooting

- If the PDF download is not a PDF, the app throws: `Downloaded content does not appear to be a PDF.`
- If the report header cannot be found, the app exits with an error:
  - `Could not find 'Weekly Trout Stocking Report: ...' header in extracted text.`
- PDF parsing is best-effort; extraction can change if the PDF layout changes.

## Development

- Extraction logic: `WeeklyTroutStockingExtractor`
- Insert logic: `Program` (`InsertSql` + Dapper)

