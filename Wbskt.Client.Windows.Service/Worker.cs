namespace Wbskt.Client.Windows.Service;

/// <summary>
/// Background service that listens for Wbskt payloads and logs them.
/// </summary>
public class Worker(ILogger<Worker> logger, WbsktListener wbsktListener, IPayloadHandler payloadHandler) : BackgroundService
{
    /// <summary>
    /// Executes the background service logic.
    /// </summary>
    /// <param name="stoppingToken">Token to signal service shutdown.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        wbsktListener.ReceivedPayloadEvent += payload => logger.LogInformation("triggered: {data}", payload.Data);
        wbsktListener.ReceivedPayloadEvent += payloadHandler.ProcessPayload;
        await wbsktListener.StartListeningAsync(stoppingToken);
    }
}
