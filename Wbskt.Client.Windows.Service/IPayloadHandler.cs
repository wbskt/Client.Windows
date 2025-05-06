using Wbskt.Client.Contracts;

namespace Wbskt.Client.Windows.Service;

public interface IPayloadHandler
{
    void ProcessPayload(UserClientPayload payload);
}
