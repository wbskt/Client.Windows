using Serilog;

namespace Wbskt.Client.Windows.Host;

public static class Program
{
    private static readonly string ProgramDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Wbskt");

    public static void Main(string[] args)
    {
        // Set up log path env
        Environment.SetEnvironmentVariable("LogPath", ProgramDataPath);
        if (!Directory.Exists(ProgramDataPath))
        {
            Directory.CreateDirectory(ProgramDataPath);
        }

        // Temporary bootstrap logger — to log before host is built
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        builder.Services.AddWindowsService();
        builder.Services.AddHostedService<Worker>();

        // Re-configure full logger (from config this time)
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);

        var host = builder.Build();

        host.Run();
    }
}
