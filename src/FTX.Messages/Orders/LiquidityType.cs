using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace FTX.Messages.Orders;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum LiquidityType
{
    [EnumMember(Value = "maker")]
    Maker,

    [EnumMember(Value = "taker")]
    Taker
}
