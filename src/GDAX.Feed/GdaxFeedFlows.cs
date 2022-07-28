using System.Text.Json;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams.Dsl;
using GDAX.Feed.Msgs;
using GDAX.Messages;
using Error = GDAX.Feed.Msgs.Error;

namespace GDAX.Feed;

/// <summary>
///     Adds additional filtering flows we need
/// </summary>
public static class GdaxFeedFlows
{
    public static Flow<string, IFeedMessage, NotUsed> JsonDeserializeFlow(ILoggingAdapter? log = null)
    {
        return Flow.FromFunction<string, IFeedMessage>(str => ParseFeedMessage(str, log)).Async();
    }

    public static IFeedMessage ParseFeedMessage(string input, ILoggingAdapter? log = null)
    {
        using var document = JsonDocument.Parse(input);
        var rootElement = document.RootElement;

        IFeedMessage msg = Empty.Instance;

        if (rootElement.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString();

            if (type == "error") return rootElement.Deserialize<Error>()!;

            if (document.RootElement.TryGetProperty("channel", out var channelElement))
            {
                var channel = channelElement.GetString();

                switch (channel)
                {
                    //case "fills":
                    //    msg = new UserFill(jtoken);
                    //    break;
                    case "ticker":
                        msg = rootElement.Deserialize<Ticker>()!;
                        break;
                    case "subscribed":
                        msg = rootElement.Deserialize<SubscribedResponse>()!;
                        break;
                    case "pong":
                        msg = rootElement.Deserialize<Heartbeat>()!;
                        break;
                    default:
                        log?.Warning("Unrecognized message type: [{0}]. Full message: {1}", type, input);
                        break;
                }
            }
        }

        return msg;
    }

    /// <summary>
    ///     Used to pipe heartbeats to a specified target actor.
    /// </summary>
    /// <remarks>
    ///     Intended to act as a trigger mechanism for rebooting a downed GDAX feed in the event that there's trouble.
    /// </remarks>
    /// <param name="heartbeatReceiver">The actor who will receive the heartbeat notifications.</param>
    /// <returns>A new flow.</returns>
    public static Flow<IFeedMessage, IFeedMessage, NotUsed> FilterHeartbeats(IActorRef heartbeatReceiver)
    {
        return Flow.FromFunction<IFeedMessage, IFeedMessage>(m => FilterHeartbeat(heartbeatReceiver, m));
    }

    private static IFeedMessage FilterHeartbeat(IActorRef heartbeatReceiver, IFeedMessage m)
    {
        if (m is Heartbeat)
        {
            heartbeatReceiver.Tell(m);
            return Empty.Instance;
        }

        return m;
    }

    public static Flow<IFeedMessage, IFeedMessage, NotUsed> FilterEmpty()
    {
        return Flow.Create<IFeedMessage>().WhereNot(FilterEmpty);
    }

    private static bool FilterEmpty(IFeedMessage m)
    {
        return m == Empty.Instance;
    }
}
