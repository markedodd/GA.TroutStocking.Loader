using GA_TroutStocking_Loader.Configuration;

namespace GA_TroutStocking_Loader.Services.Interfaces;

internal interface IAppOptionsProvider
{
    Task<AppOptions> GetAsync(CancellationToken cancellationToken = default);
}
