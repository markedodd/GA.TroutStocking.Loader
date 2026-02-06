using GA_TroutStocking_Loader.Commands;
using GA_TroutStocking_Loader.Configuration;
using GA_TroutStocking_Loader.Infrastructure;
using GA_TroutStocking_Loader.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace GA_TroutStocking_Loader.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddOptions(config)
            .AddHttpClients()
            .AddApplicationCore()
            .AddInfrastructure();

        return services;
    }

    private static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        // Commands / orchestration
        services.AddTransient<RunCommand>();
        services.AddTransient<IAppCommand>(sp => sp.GetRequiredService<RunCommand>());

        return services;
    }

    private static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppOptionsProvider, AppOptionsProvider>();
        services.AddSingleton<IEnvironmentExit, EnvironmentExit>();
        services.AddSingleton<IConsole, SystemConsole>();

        // IPdfDownloader is registered as a typed HttpClient in AddHttpClients
        services.AddTransient<IWeeklyTroutStockingExtractor, WeeklyTroutStockingExtractorAdapter>();
        services.AddTransient<IWeeklyTroutStockingWriter, WeeklyTroutStockingWriter>();

        return services;
    }

    private static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration config)
    {
        // Options pattern binding (preserve existing keys):
        // - PdfUrl (top-level)
        // - ConnectionStrings:Sql -> AppOptions.SqlConnectionString
        services.AddOptions<AppOptions>()
            .Configure(o =>
            {
                o.PdfUrl = config["PdfUrl"] ?? string.Empty;
                o.SqlConnectionString = config.GetConnectionString("Sql") ?? string.Empty;
            });

        return services;
    }

    private static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<IPdfDownloader, PdfDownloader>(c =>
            {
                c.DefaultRequestHeaders.UserAgent.ParseAdd("GaTroutStockingLoader/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

        return services;
    }
}
