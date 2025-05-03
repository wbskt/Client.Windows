using Serilog;

namespace Wbskt.Client.Windows.Service;

public static class Program
{
    // Path to store application data, typically used for logs or other persistent data.
    private static readonly string ProgramDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Wbskt");

    public static void Main(string[] args)
    {
        SetupEnvironment();

        var builder = Host.CreateApplicationBuilder(args);
        ConfigureServices(builder);
        ConfigureLogging(builder);

        var host = builder.Build();
        host.Run();
    }

    /// <summary>
    /// Sets up the environment by creating necessary directories and setting environment variables.
    /// </summary>
    private static void SetupEnvironment()
    {
        Environment.SetEnvironmentVariable("LogPath", ProgramDataPath);
        if (!Directory.Exists(ProgramDataPath))
        {
            Directory.CreateDirectory(ProgramDataPath);
        }
    }

    /// <summary>
    /// Configures application services, such as hosted services and dependency injection.
    /// </summary>
    /// <param name="builder">The application builder used to register services.</param>
    private static void ConfigureServices(HostApplicationBuilder builder)
    {
        builder.Services.AddWindowsService();
        builder.Services.AddHostedService<Worker>();
        builder.Services.AddSingleton<IPayloadHandler, PayloadHandler>();
        builder.Services.ConfigureWbsktListener(builder.Configuration);
    }

    /// <summary>
    /// Configures logging for the application using Serilog.
    /// </summary>
    /// <param name="builder">The application builder used to configure logging.</param>
    private static void ConfigureLogging(HostApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);
    }
}
