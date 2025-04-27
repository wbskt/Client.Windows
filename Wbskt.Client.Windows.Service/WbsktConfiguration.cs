using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Wbskt.Client.Windows.Service;

public class ChannelDetails
{
    public Guid SubscriberId { get; init; }
    public string Secret { get; init; } = string.Empty;
}

public class ClientDetails
{
    public string Name { get; init; } = $"{Environment.MachineName}:{Guid.NewGuid()}";
    public Guid UniqueId { get; init; }
    public int RetryIntervalInSeconds { get; init; } = 10;
}

public class WbsktConfiguration(IOptionsMonitor<WbsktConfiguration.Settings> settingsMonitor)
{
    public HostString CoreServerAddress => new(settingsMonitor.CurrentValue.CoreServerAddress);
    public ClientDetails ClientDetails => settingsMonitor.CurrentValue.ClientDetails;
    public ChannelDetails ChannelDetails => settingsMonitor.CurrentValue.ChannelDetails;

    public class Settings
    {
        public string CoreServerAddress { get; set; } = string.Empty;
        public ClientDetails ClientDetails { get; set; } = new();
        public ChannelDetails ChannelDetails { get; set; } = new();
    }
}
