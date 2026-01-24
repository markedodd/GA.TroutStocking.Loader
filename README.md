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

## Prerequisites

- .NET 8 SDK
- SQL Server (local or remote)
- A database containing table `dbo.WeeklyTroutStocking`

### Expected table schema

This project assumes a table with (at minimum) the following columns:

- `ReportDates` (string / nvarchar)
- `StockingDate` (date or string / nvarchar)
- `County` (string / nvarchar)
- `WaterBody` (string / nvarchar)

> Note: The insert logic uses `WaterBody` in SQL but the in-code parameter is `Waterbody` (mapped to `@WaterBody` in the SQL string).

## Configuration

Create an `appsettings.json` file next to the built executable (the app loads it from `AppContext.BaseDirectory`).

Example `appsettings.json`:


{
  "PdfUrl": "https://example.com/weekly_trout_stocking_report.pdf",
  "ConnectionStrings": {
    "Sql": "Server=.;Database=MyDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}


### Keys

- `PdfUrl`: URL to the Weekly Trout Stocking Report PDF.
- `ConnectionStrings:Sql`: SQL Server connection string.

## Run

From Visual Studio:

- Set the console project as the startup project.
- Run with __Debug > Start Without Debugging__.

From the command line (after building):


dotnet run --project .\GA_TroutStocking_Loader\GA_TroutStocking_Loader.csproj


The app writes parsed rows and an insert count to stdout.

## Notes / Troubleshooting

- If the PDF download is not a PDF, the app throws: `Downloaded content does not appear to be a PDF.`
- If the report header cannot be found, the app exits with an error:
  - `Could not find 'Weekly Trout Stocking Report: ...' header in extracted text.`
- PDF parsing is "best effort" because PDF text extraction depends on layout.

## Development

- The extraction logic lives in `WeeklyTroutStockingExtractor`.
- Table insert logic is in `Program` using Dapper and the `InsertSql` constant.

## License

Specify a license for this repository (e.g. MIT) or remove this section.


### Changes Made:
1. **Formatting and Structure**: Ensured consistent formatting and clear section headings for better readability.
2. **Clarity**: Added minor clarifications in the "Run" section to specify the startup project.
3. **Flow**: Maintained a logical flow from what the application does, through prerequisites, configuration, and running the application, to troubleshooting and development notes. 

This structure helps users quickly understand the purpose of the project, how to set it up, and how to troubleshoot common issues.  