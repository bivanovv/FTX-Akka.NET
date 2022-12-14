using System.Text.Json.Serialization;
using FTX.Messages;

namespace FTX.Feed.Msgs;

/// <inheritdoc />
/// <summary>
///     Abstract base class for all feed message types
/// </summary>
public record FeedMessageBase : IFeedMessage
{
    [JsonPropertyName("type")]
    public FeedMessageType Type { get; init; }
}
