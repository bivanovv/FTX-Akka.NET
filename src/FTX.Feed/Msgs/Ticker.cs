using System.Text.Json.Serialization;

namespace FTX.Feed.Msgs;

public record Ticker
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
