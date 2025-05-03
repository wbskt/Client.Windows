using Wbskt.Client.Contracts;

namespace Wbskt.Client.Windows.Service;

public interface IPayloadHandler
{
    void ProcessPayload(ClientPayload payload);
}
