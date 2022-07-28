using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GDAX.Messages.Orders;

namespace GDAX.Feed.Msgs;
//public record UserFill(
//    [property: JsonPropertyName("fee")] decimal Fee,
//    [property: JsonPropertyName("feeCurrency")] string? FeeCurrency,
//    [property: JsonPropertyName("feeRate")] decimal FeeRate,
//    [property: JsonPropertyName("future")] string? Future,
//    [property: JsonPropertyName("id")] long Id,
//    [property: JsonPropertyName("liquidity")] LiquidityType Liquidity,
//    [property: JsonPropertyName("market")] string Market,
//    [property: JsonPropertyName("baseCurrency")] string? BaseCurrency,
//    [property: JsonPropertyName("quoteCurrency")] string? QuoteCurrency,
//    [property: JsonPropertyName("orderId")] long OrderId,
//    [property: JsonPropertyName("tradeId")] long? TradeId,
//    [property: JsonPropertyName("price")] decimal Price,
//    [property: JsonPropertyName("side")] OrderSide Side,
//    [property: JsonPropertyName("size")] decimal Size,
//    [property: JsonPropertyName("time")] DateTime Time,
//    [property: JsonPropertyName("type")] string Type);
