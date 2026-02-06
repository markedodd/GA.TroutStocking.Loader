using GA_TroutStocking_Loader.Configuration;
using GA_TroutStocking_Loader.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace GA_TroutStocking_Loader.Infrastructure;

internal sealed class AppOptionsProvider(IOptions<AppOptions> options) : IAppOptionsProvider
{
    public Task<AppOptions> GetAsync(CancellationToken cancellationToken = default)
    {
        var v = options.Value;

        // Preserve existing validation/exception message semantics.
        if (string.IsNullOrWhiteSpace(v.PdfUrl) || string.IsNullOrWhiteSpace(v.SqlConnectionString))
        {
            throw new InvalidOperationException("appsettings.json is missing PdfUrl or ConnectionStrings:Sql");
        }

        return Task.FromResult(v);
    }
}
