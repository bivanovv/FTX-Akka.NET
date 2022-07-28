using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GDAX.Messages;

/// <summary>
///     The types of feed messages that can be received by the API
/// </summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum FeedMessageType
{
    [EnumMember(Value = "error")]
    Error,

    [EnumMember(Value = "subscribed")]
    Subscribed,

    [EnumMember(Value = "unsubscribed")]
    Unsubscribed,

    [EnumMember(Value = "info")]
    Info,

    [EnumMember(Value = "partial")]
    Partial,

    [EnumMember(Value = "update")]
    Update
}
