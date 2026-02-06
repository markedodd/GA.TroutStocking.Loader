using GA_TroutStocking_Loader.Services.Interfaces;

namespace GA_TroutStocking_Loader.Infrastructure;

internal sealed class EnvironmentExit : IEnvironmentExit
{
    public void Exit(int exitCode) => Environment.Exit(exitCode);
}
