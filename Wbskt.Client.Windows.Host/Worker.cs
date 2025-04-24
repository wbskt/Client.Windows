using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Wbskt.Common.Contracts;
using Wbskt.Common.Extensions;

namespace Wbskt.Client.Windows.Host;

public class Worker(ILogger<Worker> logger, WbsktConfiguration wbsktConfiguration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Client running at: {time}", DateTimeOffset.Now);
            }

            try
            {
                await Listen(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError("unexpected error: {error}", ex.Message);
                logger.LogTrace("unexpected error: {error}", ex.ToString());
            }

            await Task.Delay(5 * 1000, stoppingToken);
        }
    }

        private async Task Listen(CancellationToken ct)
        {
            var token = await GetToken();
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var socketServerAddress = jwt.Claims.GetSocketServerAddress();
            var tokenId = jwt.Claims.GetTokenId();
            logger.LogInformation("token with id: {tokenId} received", tokenId);

            ClientWebSocket? ws = null;
            try
            {
                ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("Authorization", $"Bearer {token}");

                var wsUri = new Uri($"wss://{socketServerAddress}/ws");

                logger.LogInformation("trying to connect: {wsUri}", wsUri);
                await ws.ConnectAsync(wsUri, ct);
                logger.LogInformation("connection established to: {wsUri}", wsUri);

                await ws.WriteAsync("ping", ct);

                var (receiveResult, message) = await ws.ReadAsync(ct);
                while (!receiveResult.CloseStatus.HasValue)
                {
                    HandleMessage(message);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        logger.LogInformation("closing connection ({closeStatus})", receiveResult.CloseStatusDescription);
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing connection (socket server ack)", ct);
                        break;
                    }
                    (receiveResult, message) = await ws.ReadAsync(ct); // what happens if this crashes?.
                }

                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing connection (socket server initiated close message)", ct);
            }
            catch (Exception ex)
            {
                logger.LogError("unexpected error occured during socket communication, error: {error}", ex.Message);
                logger.LogTrace("unexpected error occured during socket communication, error: {error}", ex.ToString());
            }
            finally
            {
                ws?.Dispose();
                logger.LogInformation("disposed connection");
            }
        }

        private async Task<string> GetToken()
        {
            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri($"https://{wbsktConfiguration.CoreServerAddress}"),
            };
            try
            {
                var clientConnReq = new ClientConnectionRequest()
                {
                    ChannelSecret = wbsktConfiguration.ChannelDetails.Secret,
                    ClientName = wbsktConfiguration.ClientDetails.Name,
                    ClientUniqueId = wbsktConfiguration.ClientDetails.UniqueId,
                    ChannelSubscriberId = wbsktConfiguration.ChannelDetails.SubscriberId
                };
                var result = await httpClient.PostAsync("/api/channels/client", JsonContent.Create(clientConnReq));

                return await result.Content.ReadAsStringAsync();
            }
            catch(Exception ex)
            {
                logger.LogError("cannot reach server:{baseAddress}, error: {error}", httpClient.BaseAddress, ex.Message);
                logger.LogTrace("cannot reach server:{baseAddress}, error: {error}", httpClient.BaseAddress, ex.ToString());
                throw;
            }
        }

        private void HandleMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                logger.LogWarning("received message is empty");
                return;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<ClientPayload>(message) ?? throw new JsonException("Serialized payload is null");
                payload.ProcessPayload();
            }
            catch (JsonException jex)
            {
                logger.LogError("error while deserializing payload: {message}, error: {error}", message, jex.Message);
                logger.LogError("error while deserializing payload: {message}, error: {error}", message, jex.ToString());
            }
        }
}
