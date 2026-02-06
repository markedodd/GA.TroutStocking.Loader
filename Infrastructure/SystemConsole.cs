using GA_TroutStocking_Loader.Services.Interfaces;

namespace GA_TroutStocking_Loader.Infrastructure;

internal sealed class SystemConsole : IConsole
{
    public void WriteLine(string message) => Console.WriteLine(message);

    public void WriteErrorLine(string message) => Console.Error.WriteLine(message);
}
