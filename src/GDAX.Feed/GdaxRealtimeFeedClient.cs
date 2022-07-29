using System.Diagnostics.Contracts;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GDAX.Client;
using GDAX.Client.Auth;
using GDAX.Feed.Msgs;

namespace GDAX.Feed;

/// <summary>
///     Real-time order book feed powered via websockets
/// </summary>
public class GdaxRealtimeFeedClient
{
    public GdaxRealtimeFeedClient(FtxClient httpClient)
    {
        HttpClient = httpClient;
    }

    public WebSocket Socket { get; set; }

    public FtxClient HttpClient { get; }

    public FtxEndpoints Endpoints => HttpClient.Endpoints;

    public FtxCredentials Credentials => HttpClient.Credentials;
    
    public JsonSerializerOptions SerializerSettings => HttpClient.SerializationSettings;

    public Task<WebSocket> Connect(TimeSpan? connectTimeout = null)
    {
        var cts = new CancellationTokenSource(connectTimeout ?? TimeSpan.FromSeconds(3));
        return Connect(cts.Token);
    }

    public virtual async Task<WebSocket> Connect(CancellationToken cts)
    {
        var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(Endpoints.WebsocketFeedUri), cts);
        return socket;
    }

    public async Task<WebSocket> Subscribe(string channel, string? symbol, WebSocket socket,
        CancellationToken cts, bool authenticate = true, bool heartbeat = true)
    {
        Contract.Assert(!string.IsNullOrWhiteSpace(channel), "Need to provide a channel");
        Contract.Assert(socket.State == WebSocketState.Open, "socket.State must == WebSocketState.Open");

        if (authenticate)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var loginRequest = new LoginRequest(Credentials.ApiKey, AuthenticationHelper.Sign(Credentials.Secret, $"{time}websocket_login"), time, null); // TODO: subaccount
            await SendAsync(socket, loginRequest, cts);
        }

        var subscribeRequest = new SubscribeRequest(channel, symbol);
        await SendAsync(socket, subscribeRequest, cts);
        return socket;
    }

    public async Task SendAsync(WebSocket socket, SocketRequest socketRequest, CancellationToken cts)
    {
        var json = JsonSerializer.Serialize(socketRequest, socketRequest.GetType(), SerializerSettings);
        var body = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, true, cts);
    }

    // Used for sending pings
    public async Task SendAsync(SocketRequest socketRequest)
    {
        var json = JsonSerializer.Serialize(socketRequest, socketRequest.GetType(), SerializerSettings);
        var body = Encoding.UTF8.GetBytes(json);
        await Socket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
