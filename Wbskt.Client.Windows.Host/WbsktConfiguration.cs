using Microsoft.AspNetCore.Http;

namespace Wbskt.Client.Windows.Host;

public class ChannelDetails
{
    public Guid SubscriberId { get; set; }
    public required string Secret { get; set; }
}

public class ClientDetails
{
    public required string Name { get; set; }
    public Guid UniqueId { get; set; }
    public int RetryIntervalInSeconds { get; set; } = 10;
}

public class WbsktConfiguration(ClientDetails clientDetails, ChannelDetails channelDetails, IConfiguration configuration)
{
    public HostString CoreServerAddress { get;  } = new(configuration["CoreServerAddress"]!);
    public ClientDetails ClientDetails { get; } = clientDetails;
    public ChannelDetails ChannelDetails { get; } = channelDetails;
}
