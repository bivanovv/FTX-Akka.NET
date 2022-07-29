using System.Text.Json.Serialization;

namespace FTX.Feed.Msgs;

public record SocketRequest
{
    public SocketRequest(string operation)
    {
        Operation = operation;
    }

    [JsonPropertyName("op")]
    public string Operation { get; init; }
}
