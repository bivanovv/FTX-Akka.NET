using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace FTX.Messages.Orders;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum OrderSide
{
    [EnumMember(Value = "buy")]
    Buy,

    [EnumMember(Value = "sell")]
    Sell
}
