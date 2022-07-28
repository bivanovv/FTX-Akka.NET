using System.Text.Json.Serialization;

namespace GDAX.Feed.Msgs;

public record SocketRequest
{
    public SocketRequest(string operation)
    {
        Operation = operation;
    }

    [JsonPropertyName("op")]
    public string Operation { get; init; }
}
