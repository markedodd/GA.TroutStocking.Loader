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
- Generic Host (`Microsoft.Extensions.Hosting`)
- Dependency Injection (`Microsoft.Extensions.DependencyInjection`)
- Options pattern (`Microsoft.Extensions.Options`)
- [Dapper](https://github.com/DapperLib/Dapper)
- [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)
- [UglyToad.PdfPig](https://github.com/UglyToad/PdfPig)
- log4net (structured JSON messages via `Logging/LogJson`)

## Solution organization methodology

This solution follows a “console app + Generic Host” organization where:

- `Program` is the composition root (builds the host and runs a single command).
- Application flow is implemented as a command (`Commands/`) which orchestrates work using injected dependencies.
- Infrastructure concerns are behind small interfaces (`Services/Interfaces/`) and implemented in `Infrastructure/`.
- Configuration uses the Options pattern (`Configuration/`).

> Note: the `/src` + `/tests` physical folder split is planned for later. For now, these folders exist within the root project layout.

### Key folders

- `Commands/`
  - Defines the app entry workflow.
  - `RunCommand` implements `IAppCommand`.

- `Configuration/`
  - Options bound from host configuration.
  - `AppOptions` provides `PdfUrl` and `SqlConnectionString` (bound from `PdfUrl` and `ConnectionStrings:Sql`).

- `Infrastructure/`
  - External system interactions:
    - `PdfDownloader` (HTTP download via typed `HttpClient`)
    - `WeeklyTroutStockingWriter` (SQL insert via Dapper)
    - `WeeklyTroutStockingExtractorAdapter` (adapter over the static PDF extractor)
    - `SystemConsole` (console output)
    - `EnvironmentExit` (process exit)

- `Services/Interfaces/`
  - Small abstractions to isolate infrastructure and improve testability:
    - `IPdfDownloader`, `IWeeklyTroutStockingWriter`, `IWeeklyTroutStockingExtractor`
    - `IConsole`, `IEnvironmentExit`
    - config access: `IAppOptionsProvider`

- `Extensions/`
  - DI registration entry point:
    - `AddApplicationServices(this IServiceCollection services, IConfiguration config)`

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

## Database

### Expected table schema

This project assumes a table with (at minimum) the following columns:

- `ReportDates` (string / nvarchar)
- `StockingDate` (date or string / nvarchar)
- `County` (string / nvarchar)
- `WaterBody` (string / nvarchar)

> Note: the insert statement uses parameters `@ReportDates`, `@StockingDate`, `@County`, and `@WaterBody`. Ensure the parameter names supplied by the app match these names.

## Run

From Visual Studio:

- Set the console project as the startup project.
- Run with **Debug > Start Without Debugging**.

From the command line:

```shell
dotnet run --project .\GA.TroutStocking.Loader.csproj
```

The app writes parsed rows and an insert count to stdout.

## Notes / Troubleshooting

- If the PDF download is not a PDF, the app throws: `Downloaded content does not appear to be a PDF.`
- If the report header cannot be found, the app exits with an error:
  - `Could not find 'Weekly Trout Stocking Report: ...' header in extracted text.`
- PDF parsing is best-effort; extraction can change if the PDF layout changes.

## Testing methodology

Tests are written using xUnit (`GA.TroutStocking.Loader.Tests`).

### Approach

- Prefer **Host/DI-based tests** for orchestration:
  - Tests build a host using the same DI registration as production (`AddApplicationServices(...)`).
  - Tests override specific dependencies (HTTP/SQL/PDF parsing/console/exit) with fakes.
  - This verifies both behavior and DI wiring.

- Prefer small abstractions for process-wide/static dependencies:
  - `IConsole` avoids `Console.SetOut` / `Console.SetError` in tests.
  - `IEnvironmentExit` prevents tests from terminating the test runner.

### InternalsVisibleTo

Production types are `internal` by default. The test project is granted access using:

- `Properties/AssemblyInfo.cs`: `[assembly: InternalsVisibleTo("GA.TroutStocking.Loader.Tests")]`

This keeps the production API surface small while maintaining fast, maintainable tests.

### Commands

```shell
dotnet build .\GA.TroutStocking.Loader.sln

dotnet test .\GA.TroutStocking.Loader.Tests\GA.TroutStocking.Loader.Tests.csproj

