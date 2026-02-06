using GA_TroutStocking_Loader.Commands;
using GA_TroutStocking_Loader.Configuration;
using GA_TroutStocking_Loader.Services.Interfaces;
using GA_TroutStocking_Loader.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using PDFExtraction;
using Xunit;

namespace GA_TroutStocking_Loader.Tests.Commands;

public sealed class RunCommandTests
{
    [Fact]
    public async Task ExecuteAsync_HappyPath_WritesInsertedCountToStdout()
    {
        var console = new FakeConsole();
        var envExit = new FakeEnvironmentExit();

        using var host = TestHostFactory.CreateHost(services =>
        {
            services.Replace<IAppOptionsProvider>(
                new FakeOptionsProvider(new AppOptions
                {
                    PdfUrl = "http://example.test/report.pdf",
                    SqlConnectionString = "Server=.;Database=Db;Trusted_Connection=True"
                }));

            services.Replace<IPdfDownloader>(new FakePdfDownloader("C:\\temp\\fake.pdf"));

            services.Replace<IWeeklyTroutStockingExtractor>(
                new FakeExtractor(
                    reportDates: "12/15/2025 - 12/19/2025",
                    rows: new List<WeeklyTroutStockingExtractor.StockingRow>
                    {
                        new("12/15/2025", "Forsyth", "Lanier Tailwater")
                    }));

            services.Replace<IWeeklyTroutStockingWriter>(new FakeWriter(inserted: 7));
            services.Replace<IEnvironmentExit>(envExit);
            services.Replace<IConsole>(console);
        });

        var sut = host.Services.GetRequiredService<IAppCommand>();

        await sut.ExecuteAsync();

        Assert.Contains("Inserted new rows: 7", console.StdOutLines);
        Assert.Empty(console.StdErrLines);
        Assert.Null(envExit.LastExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReportDatesMissing_WritesStderrAndExits1()
    {
        var console = new FakeConsole();
        var envExit = new FakeEnvironmentExit();

        using var host = TestHostFactory.CreateHost(services =>
        {
            services.Replace<IAppOptionsProvider>(
                new FakeOptionsProvider(new AppOptions
                {
                    PdfUrl = "http://example.test/report.pdf",
                    SqlConnectionString = "Server=.;Database=Db;Trusted_Connection=True"
                }));

            services.Replace<IPdfDownloader>(new FakePdfDownloader("C:\\temp\\fake.pdf"));

            services.Replace<IWeeklyTroutStockingExtractor>(
                new FakeExtractor(
                    reportDates: string.Empty,
                    rows: new List<WeeklyTroutStockingExtractor.StockingRow>()));

            services.Replace<IWeeklyTroutStockingWriter>(new FakeWriter(inserted: 0));
            services.Replace<IEnvironmentExit>(envExit);
            services.Replace<IConsole>(console);
        });

        var sut = host.Services.GetRequiredService<IAppCommand>();

        await sut.ExecuteAsync();

        Assert.Contains("Could not find 'Weekly Trout Stocking Report: ...' header in extracted text.", console.StdErrLines);
        Assert.Equal(1, envExit.LastExitCode);
    }

    private sealed class FakeOptionsProvider(AppOptions options) : IAppOptionsProvider
    {
        public Task<AppOptions> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(options);
    }

    private sealed class FakePdfDownloader(string filePath) : IPdfDownloader
    {
        public Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default) => Task.FromResult(filePath);
    }

    private sealed class FakeExtractor(string reportDates, List<WeeklyTroutStockingExtractor.StockingRow> rows) : IWeeklyTroutStockingExtractor
    {
        public Task<(string reportDates, List<WeeklyTroutStockingExtractor.StockingRow> rows)> ExtractAsync(
            string pdfPath,
            CancellationToken cancellationToken = default)
            => Task.FromResult((reportDates, rows));
    }

    private sealed class FakeWriter(int inserted) : IWeeklyTroutStockingWriter
    {
        public Task<int> InsertNewRowsAsync(
            string reportDates,
            IReadOnlyCollection<WeeklyTroutStockingExtractor.StockingRow> rows,
            CancellationToken cancellationToken = default) => Task.FromResult(inserted);
    }

    private sealed class FakeEnvironmentExit : IEnvironmentExit
    {
        public int? LastExitCode { get; private set; }

        public void Exit(int exitCode) => LastExitCode = exitCode;
    }

    private sealed class FakeConsole : IConsole
    {
        public List<string> StdOutLines { get; } = new();
        public List<string> StdErrLines { get; } = new();

        public void WriteLine(string message) => StdOutLines.Add(message);

        public void WriteErrorLine(string message) => StdErrLines.Add(message);
    }
}
