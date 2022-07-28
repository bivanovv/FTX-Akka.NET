using System.Text.Json.Serialization;

namespace GDAX.Feed.Msgs;

/// <summary>
///     If you send a message that is not recognized or an error occurs,
///     the error message will be sent and you will be disconnected.
/// </summary>
public sealed record Error : FeedMessageBase
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Message { get; set; } = null!;
}
