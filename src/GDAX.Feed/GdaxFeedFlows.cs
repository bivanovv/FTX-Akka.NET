using System.Text.Json;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams.Dsl;
using FTX.Messages;
using GDAX.Feed.Msgs;
using Error = GDAX.Feed.Msgs.Error;

namespace GDAX.Feed;

/// <summary>
///     Adds additional filtering flows we need
/// </summary>
public static class GdaxFeedFlows
{
    public static Flow<IFeedMessage, IFeedMessage, NotUsed> FilterEmpty()
    {
        return Flow.Create<IFeedMessage>().WhereNot(FilterEmpty);
    }

    /// <summary>
    ///     Used to pipe heartbeats to a specified target actor.
    /// </summary>
    /// <remarks>
    ///     Intended to act as a trigger mechanism for rebooting a downed FTX feed in the event that there's trouble.
    /// </remarks>
    /// <param name="heartbeatReceiver">The actor who will receive the heartbeat notifications.</param>
    /// <returns>A new flow.</returns>
    public static Flow<IFeedMessage, IFeedMessage, NotUsed> FilterHeartbeats(IActorRef heartbeatReceiver)
    {
        return Flow.FromFunction<IFeedMessage, IFeedMessage>(m => FilterHeartbeat(heartbeatReceiver, m));
    }

    public static Flow<string, IFeedMessage, NotUsed> JsonDeserializeFlow(JsonSerializerOptions jsonSerializerOptions, ILoggingAdapter? log = null)
    {
        return Flow.FromFunction<string, IFeedMessage>(str => ParseFeedMessage(str, jsonSerializerOptions, log)).Async();
    }

    private static IFeedMessage ParseFeedMessage(string input, JsonSerializerOptions jsonSerializerOptions, ILoggingAdapter? log = null)
    {
        using var document = JsonDocument.Parse(input);
        var rootElement = document.RootElement;

        IFeedMessage msg = Empty.Instance;

        if (rootElement.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString();

            if (type != "update" && type != "partial")
            {
                switch (type)
                {
                    case "error":
                        msg = rootElement.Deserialize<Error>(jsonSerializerOptions)!;
                        break;
                    case "subscribed":
                        msg = rootElement.Deserialize<SubscribedResponse>(jsonSerializerOptions)!;
                        break;
                    case "pong":
                        msg = rootElement.Deserialize<Heartbeat>(jsonSerializerOptions)!;
                        break;
                    default:
                        log?.Warning("Unrecognized message type: [{0}]. Full message: {1}", type, input);
                        break;
                }
            }
            else if (document.RootElement.TryGetProperty("channel", out var channelElement))
            {
                var channel = channelElement.GetString();

                switch (channel)
                {
                    case "fills":
                        msg = rootElement.Deserialize<UserFill>(jsonSerializerOptions)!;
                        break;
                    case "ticker":
                        msg = rootElement.Deserialize<Ticker>(jsonSerializerOptions)!;
                        break;
                    default:
                        log?.Warning("Unrecognized message channel: [{0}]. Full message: {1}", channel, input);
                        break;
                }
            }
        }

        return msg;
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

    private static bool FilterEmpty(IFeedMessage m)
    {
        return m == Empty.Instance;
    }
}
