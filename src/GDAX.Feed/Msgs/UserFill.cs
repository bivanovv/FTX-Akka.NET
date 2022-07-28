using System.Text.Json.Serialization;
using GDAX.Messages.Orders;

namespace GDAX.Feed.Msgs;

public record UserFill : FeedMessageBase
{
    [JsonPropertyName("channel")]
    public string Channel { get; init; } = null!;

    [JsonPropertyName("market")]
    public string Market { get; init; } = null!;

    [JsonPropertyName("data")]
    public UserFillData Data { get; init; } = null!;
}

public record UserFillData(
    [property: JsonPropertyName("fee")] decimal Fee,
    [property: JsonPropertyName("feeCurrency")] string? FeeCurrency,
    [property: JsonPropertyName("feeRate")] decimal FeeRate,
    [property: JsonPropertyName("future")] string? Future,
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("liquidity")] LiquidityType Liquidity,
    [property: JsonPropertyName("market")] string Market,
    [property: JsonPropertyName("baseCurrency")] string? BaseCurrency,
    [property: JsonPropertyName("quoteCurrency")] string? QuoteCurrency,
    [property: JsonPropertyName("orderId")] long OrderId,
    [property: JsonPropertyName("tradeId")] long? TradeId,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("side")] OrderSide Side,
    [property: JsonPropertyName("size")] decimal Size,
    [property: JsonPropertyName("time")] DateTime Time,
    [property: JsonPropertyName("type")] string Type);
