using System.Text.Json.Serialization;

namespace FTX.Feed.Msgs;

public sealed record SubscribedResponse: FeedMessageBase
{
    [JsonPropertyName("channel")]
    public string Channel { get; init; } = null!;

    [JsonPropertyName("market")]
    public string? Market { get; init; }
}
