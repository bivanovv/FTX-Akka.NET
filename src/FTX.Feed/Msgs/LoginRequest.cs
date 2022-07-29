using System.Text.Json.Serialization;

namespace FTX.Feed.Msgs;

public record LoginRequest : SocketRequest
{
    public LoginRequest(string key, string sign, long time, string? subaccount) : base("login")
    {
        Parameters = new LoginParams
        {
            Key = key,
            Sign = sign,
            Time = time,
            Subaccount = subaccount
        };
    }

    [JsonPropertyName("args")]
    public LoginParams Parameters { get; init; }
}

public record LoginParams
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = null!;

    [JsonPropertyName("subaccount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Subaccount { get; init; }

    [JsonPropertyName("sign")]
    public string Sign { get; init; } = null!;

    [JsonPropertyName("time")]
    public long Time { get; init; }
}
