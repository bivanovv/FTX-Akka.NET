using FTX.Messages;

namespace GDAX.Feed.Msgs;

/// <inheritdoc />
/// <summary>
///     INTERNAL API.
///     Used when we parse an unknown message type.
/// </summary>
/// <remarks>
///     Filtered out by our JSON parsing stage. Never propagated over network.
/// </remarks>
internal sealed class Empty : IFeedMessage
{
    public static readonly Empty Instance = new();

    private Empty()
    {
    }

    public FeedMessageType Type => FeedMessageType.Error;
}
