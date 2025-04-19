using System.Net.Http.Json;
using System.Net.WebSockets;
using Wbskt.Common.Contracts;
using Wbskt.Common.Extensions;

namespace Wbskt.Client.Windows.Host;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            try
            {
                await Listen(stoppingToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

        private async Task Listen(CancellationToken ct)
        {
            var token = await GetToken();
            ClientWebSocket? ws = null;
            try
            {
                ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("Authorization", $"Bearer {token}");

                var wsUri = new Uri($"wss://localhost:7293/ws");

                logger.LogInformation("[Connection] trying to connect \"{wsUri}\"", wsUri);
                await ws.ConnectAsync(wsUri, ct);
                logger.LogInformation("[Connection] connection established to \"{wsUri}\"", wsUri);

                await ws.WriteAsync("ping", ct);

                var (receiveResult, message) = await ws.ReadAsync(ct);
                while (!receiveResult.CloseStatus.HasValue)
                {
                    logger.LogInformation("[Message] received message: \"{message}\"", message);
                    HandleMessage(message);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        logger.LogInformation("[Connection] closing connection (remote server initiated close message)");
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, receiveResult.CloseStatusDescription, ct);
                        break;
                    }
                    (receiveResult, message) = await ws.ReadAsync(ct); // what happens if this crashes?.
                }

                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing connection (remote server initiated close message)", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                logger.LogTrace(new EventId(1), ex, ex.Message);
            }
            finally
            {
                ws?.Dispose();
                logger.LogInformation("[Connection] disposed connection");
            }
        }

        private async Task<string> GetToken()
        {
            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://localhost:7020"),
            };
            HttpResponseMessage? result = null;
            try
            {
                var clientConnReq = new ClientConnectionRequest()
                {
                    ChannelSecret = "thisasdf is the very veyr bug secret",
                    ClientName = "boo",
                    ClientUniqueId = Guid.NewGuid(),
                    ChannelSubscriberId = Guid.Parse("8c2cdf59-3b9d-43ce-9c89-b842c63080df")
                };
                result = await httpClient.PostAsync("/api/channels/client", JsonContent.Create(clientConnReq));

                return await result.Content.ReadAsStringAsync();
            }
            catch(Exception ex)
            {
                logger.LogDebug("cannot reach server:{baseAddress}", httpClient.BaseAddress);
                throw;
            }
        }

        private void HandleMessage(string message)
        {
            logger.LogInformation(message);
        }
}
