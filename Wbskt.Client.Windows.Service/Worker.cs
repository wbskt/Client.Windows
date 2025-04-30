namespace Wbskt.Client.Windows.Service;

public class Worker(ILogger<Worker> logger, WbsktListener wbsktListener) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        wbsktListener.ReceivedPayload += payload => logger.LogInformation("triggered: {data}", payload.Data);
        await wbsktListener.StartListening(stoppingToken);
    }
}
