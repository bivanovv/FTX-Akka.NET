using System.Text.Json.Serialization;

namespace GDAX.Feed.Msgs;

public record Ticker: FeedMessageBase
{
    [JsonPropertyName("channel")]
    public string Channel { get; init; } = null!;

    [JsonPropertyName("market")]
    public string Market { get; init; } = null!;

    [JsonPropertyName("data")]
    public TickerData Data { get; init; } = null!;
}

public record TickerData
{
    [JsonPropertyName("ask")]
    public decimal? BestAskPrice { get; init; }

    [JsonPropertyName("bidSize")]
    public decimal? BestBidQuantity { get; init; }

    [JsonPropertyName("bid")]
    public decimal? BestBidPrice { get; init; }

    [JsonPropertyName("askSize")]
    public decimal? BestAskQuantity { get; init; }

    [JsonPropertyName("last")]
    public decimal? LastPrice { get; init; }

    //[JsonPropertyName("time")]
    //[JsonConverter(typeof(UnixEpochDateTimeConverter))]
    //public DateTime Timestamp { get; init; }
}
