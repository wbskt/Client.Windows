using Microsoft.AspNetCore.Http;

namespace Wbskt.Client.Windows.Host;

public class ChannelDetails
{
    public Guid SubscriberId { get; init; }
    public required string Secret { get; init; } = string.Empty;
}

public class ClientDetails
{
    public required string Name { get; init; } = $"{Environment.MachineName}:{Guid.NewGuid()}";
    public Guid UniqueId { get; init; }
    public int RetryIntervalInSeconds { get; init; } = 10;
}

public class WbsktConfiguration(ClientDetails clientDetails, ChannelDetails channelDetails, IConfiguration configuration)
{
    public HostString CoreServerAddress { get;  } = new(configuration["CoreServerAddress"]!);
    public ClientDetails ClientDetails { get; } = clientDetails;
    public ChannelDetails ChannelDetails { get; } = channelDetails;
}
