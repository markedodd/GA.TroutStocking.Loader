using GA_TroutStocking_Loader.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GA_TroutStocking_Loader.Tests.TestHelpers;

internal static class TestHostFactory
{
    public static IHost CreateHost(Action<IServiceCollection> overrideServices)
    {
        var builder = Host.CreateApplicationBuilder();

        // Provide minimal in-memory config so options binding has defaults.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PdfUrl"] = "http://example.test/report.pdf",
            ["ConnectionStrings:Sql"] = "Server=.;Database=Db;Trusted_Connection=True"
        });

        builder.Services.AddApplicationServices(builder.Configuration);

        // Allow tests to replace registrations.
        overrideServices(builder.Services);

        return builder.Build();
    }

    public static void Replace<TService>(this IServiceCollection services, TService implementation)
        where TService : class
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(TService))
            {
                services.RemoveAt(i);
            }
        }

        services.AddSingleton(typeof(TService), implementation);
    }

    public static void Replace<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(TService))
            {
                services.RemoveAt(i);
            }
        }

        services.AddSingleton<TService, TImplementation>();
    }
}
