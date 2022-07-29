using System.Text.Json.Serialization;

namespace FTX.Feed.Msgs;

public record SocketResponse<T> : FeedMessageBase where T : class 
{
    [JsonPropertyName("channel")]
    public string Channel { get; init; } = null!;

    [JsonPropertyName("market")]
    public string? Market { get; init; }

    [JsonPropertyName("data")]
    public T Data { get; init; } = null!;
}
