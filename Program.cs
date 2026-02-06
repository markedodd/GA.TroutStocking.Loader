using GA_TroutStocking_Loader.Commands;
using GA_TroutStocking_Loader.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GA_TroutStocking_Loader
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Initialize(AppContext.BaseDirectory);

            var builder = Host.CreateApplicationBuilder(args);

            // Preserve existing behavior: appsettings.json is expected alongside the built executable.
            // We add that location explicitly as a configuration source.
            builder.Configuration.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                optional: true,
                reloadOnChange: false);

            builder.Services.AddApplicationServices(builder.Configuration);

            using var host = builder.Build();

            var cmd = host.Services.GetRequiredService<IAppCommand>();
            await cmd.ExecuteAsync();
        }
    }
}