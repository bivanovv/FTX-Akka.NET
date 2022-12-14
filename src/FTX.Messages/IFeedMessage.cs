namespace FTX.Messages;

/// <summary>
///     Interface for all messages that can be received on the live order book for FTX
/// </summary>
public interface IFeedMessage
{
    FeedMessageType Type { get; }
}
