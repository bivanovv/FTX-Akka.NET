using System.Text.Json.Serialization;

namespace FTX.Feed.Msgs;

public record SubscribeRequest : SocketRequest
{
    public SubscribeRequest(string channel, string? symbol) : base("subscribe")
    {
        Channel = channel;
        Market = symbol;
    }

    [JsonPropertyName("channel")]
    public string Channel { get; init; }

    [JsonPropertyName("market")]
    public string? Market { get; init; }
}
