using log4net;
using log4net.Config;
using System.Reflection;

namespace GA_TroutStocking_Loader;

internal static class Log
{
    public static ILog Logger { get; private set; } = LogManager.GetLogger(typeof(Program));

    public static void Initialize(string baseDirectory)
    {
        Directory.CreateDirectory(Path.Combine(baseDirectory, "logs"));

        var configPath = Path.Combine(baseDirectory, "log4net.config");
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("Missing log4net.config.", configPath);
        }

        var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(repository, new FileInfo(configPath));

        Logger = LogManager.GetLogger(typeof(Program));

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.Fatal(LogJson.Message("Unhandled exception"), ex);
            }
            else
            {
                Logger.Fatal(LogJson.Message("Unhandled exception (non-Exception)"));
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Logger.Fatal(LogJson.Message("Unobserved task exception"), e.Exception);
            e.SetObserved();
        };
    }

    // log4net has no native TRACE; map to DEBUG and label in the TraceFile appender.
    public static void Trace(string messageJson) => Logger.Debug(messageJson);
}