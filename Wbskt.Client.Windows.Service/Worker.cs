using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Wbskt.Common.Contracts;
using Wbskt.Common.Extensions;

namespace Wbskt.Client.Windows.Service;

public class Worker(ILogger<Worker> logger, WbsktConfiguration wbsktConfiguration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!IsConfigValid(wbsktConfiguration))
            {
                logger.LogError("restart the service after configuring properly");
                await Task.Delay(wbsktConfiguration.ClientDetails.RetryIntervalInSeconds * 1000, stoppingToken);
                continue;
            }

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

            await Task.Delay(wbsktConfiguration.ClientDetails.RetryIntervalInSeconds * 1000, stoppingToken);
        }
    }

    private async Task Listen(CancellationToken ct)
    {
        var token = await GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        var socketServerAddress = jwt.Claims.GetSocketServerAddress();
        var tokenId = jwt.Claims.GetTokenId();
        logger.LogInformation("token with id: {tokenId} received", tokenId);

        ClientWebSocket? ws = null;
        try
        {
            ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            ws.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;

            var wsUri = new Uri($"ws://{socketServerAddress}/ws");

            logger.LogInformation("trying to connect: {wsUri}", wsUri);
            await ws.ConnectAsync(wsUri, ct);
            logger.LogInformation("connection established to: {wsUri}", wsUri);

            await ws.WriteAsync("ping", ct);

            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                var (receiveResult, message) = await ws.ReadAsync(ct);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("closing connection ({closeStatus})", receiveResult.CloseStatusDescription);
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing connection (socket server ack)", ct);
                    break;
                }

                HandleMessage(message);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation token is cancelled.
            logger.LogInformation("cancellation requested, closing socket connection.");
        }
        catch (WebSocketException ex) when (ct.IsCancellationRequested)
        {
            // Sometimes WebSocket throws when cancelled mid-await
            logger.LogInformation("webSocket cancelled, closing gracefully. {error}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "unexpected error occured during socket communication, error: {error}", ex.Message);
        }
        finally
        {
            if (ws?.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cancellation requested", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("error while closing websocket: {error}", ex.Message);
                }
            }

            ws?.Dispose();
            logger.LogInformation("disposed connection");
        }
    }


    private async Task<string> GetToken()
    {
        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri($"http://{wbsktConfiguration.CoreServerAddress}"),
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

            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadAsStringAsync();
            }

            logger.LogError("response from token request is: {message}, statusCode: {code}", result.ReasonPhrase, result.StatusCode);
            return string.Empty;
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "cannot reach server:{baseAddress}, error: {error}", httpClient.BaseAddress, ex.Message);
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
            logger.LogInformation("payload received: {payload}", payload.Data);
            payload.ProcessPayload();
        }
        catch (JsonException jex)
        {
            logger.LogError(jex, "error while deserializing payload: {message}, error: {error}", message, jex.Message);
        }
    }

    private bool IsConfigValid(WbsktConfiguration? configuration)
    {
        if (configuration == null)
        {
            logger.LogError("configuration is not set");
            return false;
        }

        if (configuration.ChannelDetails.SubscriberId == default)
        {
            logger.LogError("subscriberId is not set");
            return false;
        }

        if (string.IsNullOrWhiteSpace(configuration.ChannelDetails.Secret))
        {
            logger.LogError("secret is not set");
            return false;
        }

        if (configuration.ClientDetails.UniqueId == default)
        {
            logger.LogError("uniqueId is not set");
            return false;
        }

        return true;
    }
}
