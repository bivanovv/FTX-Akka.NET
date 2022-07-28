using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Actors;
using Akka.Streams.Dsl;
using GDAX.Client;
using GDAX.Messages;

namespace GDAX.Feed;

/// <summary>
///     Used to create new real-time Ftx feeds
/// </summary>
public sealed class GdaxFeed
{
    public GdaxFeed(IActorRefFactory system, GdaxRealtimeFeedClient realtimeClient)
    {
        System = system;
        RealtimeClient = realtimeClient;
    }

    public IActorRefFactory System { get; }
    
    public GdaxRealtimeFeedClient RealtimeClient { get; }
    
    public (Source<IFeedMessage, NotUsed> source, IActorRef sourceActor) Subscribe(string channel, string? market,
        bool authenticate, bool heartbeat, string? feedName = null,
        ILoggingAdapter? log = null)
    {
        return Subscribe(channel, market, DefaultFeedPublisherSettings.DefaultHeartbeatInterval, authenticate, heartbeat, feedName,
            log);
    }

    public (Source<IFeedMessage, NotUsed> source, IActorRef sourceActor) Subscribe(string channel, string? market,
        TimeSpan heartbeatInterval, bool authenticate, bool heartbeat, string? feedName = null, ILoggingAdapter? log = null)
    {
        var settings = new DefaultFeedPublisherSettings
        {
            HeartbeatInterval = heartbeatInterval,
            TrackHeartbeats = heartbeat,
            AuthenticateFeed = authenticate,
            Client = RealtimeClient,
            Channel = channel,
            Market = market,
            ConnectTimeout = DefaultFeedPublisherSettings.DefaultConnectTimeout
        };

        return Subscribe(settings, feedName, log);
    }

    public (Source<IFeedMessage, NotUsed> source, IActorRef sourceActor) Subscribe(IFeedPublisherSettings settings, string? feedName = null, ILoggingAdapter? log = null)
    {
        var publisherRef =
            System.ActorOf(Props.Create(() => new GdaxFeedPublisher(settings)), feedName);

        var publisher =
            Source.FromPublisher(ActorPublisher.Create<string>(publisherRef))
                .MapMaterializedValue(_ => publisherRef);

        var graph = CreateGraph(publisher, settings.TrackHeartbeats, publisherRef, log);

        var s = Source.FromGraph(graph);

        return (s, publisherRef);
    }

    internal static IGraph<SourceShape<IFeedMessage>, NotUsed> CreateGraph<TMat>(Source<string, TMat> publisher,
        bool heartbeat = false, IActorRef? heartbeatTarget = null, ILoggingAdapter? log = null)
    {
        var graph = GraphDsl.Create(builder =>
        {
            var origSource = builder.Add(publisher);
            var filter = builder.Add(Flow.Create<string, string>().WhereNot(string.IsNullOrEmpty));
            var serializer = builder.Add(GdaxFeedFlows.JsonDeserializeFlow(log));

            builder.From(origSource.Outlet).To(filter.Inlet);
            builder.From(filter.Outlet).To(serializer.Inlet);

            var emptyFilter = builder.Add(GdaxFeedFlows.FilterEmpty());

            // add heartbeat detection capabilities if requested
            if (heartbeat && heartbeatTarget != null)
            {
                var heartbeatFilter = builder.Add(GdaxFeedFlows.FilterHeartbeats(heartbeatTarget));
                builder.From(serializer.Outlet).To(heartbeatFilter.Inlet);
                builder.From(heartbeatFilter.Outlet).To(emptyFilter.Inlet);
            }
            else
            {
                builder.From(serializer.Outlet).To(emptyFilter.Inlet);
            }

            return new SourceShape<IFeedMessage>(emptyFilter.Outlet);
        });

        return graph;
    }
}
