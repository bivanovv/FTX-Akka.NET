using System.Diagnostics.Contracts;
using Akka.Actor;
using Akka.Event;
using Akka.Streams.Actors;
using FTX.Feed.Msgs;

namespace FTX.Feed;

/// <summary>
///     Published used for FTX live order book feed
/// </summary>
public sealed class FtxFeedPublisher : ActorPublisher<string>, IWithUnboundedStash
{
    private readonly bool _heartbeat;

    private readonly IFeedPublisherSettings _settings;
    private readonly TimeSpan _connectTimeout;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly FtxRealtimeFeedClient _realTime;

    /// <summary>
    ///     If we haven't received a heartbeat within this interval, we assume that the underlying feed has been poisoned and
    ///     needs to be rebooted.
    /// </summary>
    private readonly TimeSpan _heartbeatInterval;

    // full messages pulled from the byte array
    private readonly LinkedList<string> _messages = new();

    private DateTime _lastHeartbeat = DateTime.UtcNow;

    private IActorRef? _feedReader;
    private ICancelable? _sendPing;
    private ICancelable? _heartbeatCheck;
    private ICancelable? _connectCheck;

    public FtxFeedPublisher(FtxRealtimeFeedClient client, TimeSpan connectTimeout, string channel, bool authenticate = false, bool heartbeat = true,
        int bufferSize = DefaultFeedPublisherSettings.DefaultBufferSize)
        : this(new DefaultFeedPublisherSettings
            { AuthenticateFeed = authenticate, TrackHeartbeats = heartbeat, BufferSize = bufferSize, ConnectTimeout = connectTimeout, Client = client, Channel = channel })
    {
    }

    public FtxFeedPublisher(IFeedPublisherSettings settings)
    {
        Contract.Requires(settings != null);
        Contract.Requires(settings.Client != null);
        Contract.Assert(!string.IsNullOrWhiteSpace(settings.Channel));
        Contract.Assert(settings.BufferSize >= 2048, "bufferSize > 2048 buffer must be at least 2kb in size.");
        _settings = settings;
        _connectTimeout = settings.ConnectTimeout;
        _heartbeatInterval = settings.HeartbeatInterval;
        _heartbeat = settings.TrackHeartbeats;
        _realTime = settings.Client;
    }

    /// <summary>
    ///     Returns <c>true</c> if we're in an viable heartbeat stage. <c>false</c> if we've missed too many heartbeats.
    /// </summary>
    public bool HeartbeatOk => DateTime.UtcNow - _lastHeartbeat <= _heartbeatInterval;

    /// <summary>
    ///     Returns <c>true</c> if there is any demand on the publisher from downstream consumers.
    ///     <c>false</c> otherwise.
    /// </summary>
    public bool HasDemand => TotalDemand > 0;

    public IStash Stash { get; set; }

    #region SupervisionStrategy

    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(ex =>
        {
            _log.Error(ex, "encountered error from underlying feed reader. Restarting...");
            BecomeRestarting(false);
            return Directive.Stop;
        }, false);
    }

    #endregion

    private void BecomeConnecting()
    {
        Become(Connecting);
        _connectCheck = Context.System.Scheduler.ScheduleTellOnceCancelable(_connectTimeout, Self,
            ConnectTimeout.Instance, ActorRefs.NoSender);
    }

    protected override void PreStart()
    {
        BecomeConnecting();
        _feedReader = SpawnFtxFeedReader();
    }

    protected override void PostStop()
    {
        _connectCheck?.Cancel();
        _sendPing?.Cancel();
        _heartbeatCheck?.Cancel();
    }

    private bool Connecting(object message)
    {
        switch (message)
        {
            case FtxFeedReader.Subscribed s:
                // we've successfully subscribed to the feed
                if (_heartbeat)
                {
                    _sendPing = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(_heartbeatInterval, 
                        _heartbeatInterval, Self, SendPing.Instance, ActorRefs.NoSender);

                    _heartbeatCheck = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(_heartbeatInterval,
                        _heartbeatInterval, Self, CheckHeartbeat.Instance, ActorRefs.NoSender);

                    UpdateHeartbeat();
                }

                _connectCheck?.Cancel();
                _connectCheck = null; // leave it open for GC
                Become(Receive);
                Stash.UnstashAll();
                break;
            case ConnectTimeout t:
                _log.Warning("Failed to connect to FTX within [{0}]. Restarting.", _connectTimeout);
                BecomeRestarting(false);
                break;
            default:
                Stash.Stash();
                break;
        }

        return true;
    }

    private bool WaitingForTermination(object message)
    {
        switch (message)
        {
            case Terminated t:
                _log.Debug("Received termination notice for FTX Feed Reader.");
                BecomeRestarting();
                break;
            default:
                Stash.Stash();
                break;
        }

        return true;
    }

    protected override bool Receive(object message)
    {
        switch (message)
        {
            case SocketData d:
                UpdateHeartbeat(); // update heartbeat if we receive ANY message
                if (_messages.Count == 0 && HasDemand)
                {
                    OnNext(d.Data);
                }
                else // always want to flush the buffer
                {
                    _messages.AddLast(d.Data);
                    DeliverBuffer();
                }

                break;
            case Request req:
                DeliverBuffer();
                break;
            case Cancel cancel:
                // do nothing
                break;
            case SendPing sendPing:
                // Send ping
                Console.WriteLine("Sending ping " + DateTime.UtcNow);
                _realTime.SendAsync(new SocketRequest("ping"));
                break;
            case CheckHeartbeat check when HeartbeatOk:
                // do nothing
                break;
            case CheckHeartbeat check when !HeartbeatOk: // FAILED. HEARTBEATS OVERDUE
                _log.Error("ERROR: haven't received heartbeat from FTX in [{0}]. Rebooting socket.", _heartbeatInterval);
                BecomeRestarting(false);
                break;
            case Heartbeat heartbeat:
                UpdateHeartbeat();
                break;
            case Complete _:
                Context.Unwatch(_feedReader); // unsubscribe from deathwatch
                OnCompleteThenStop();
                break;
            case Terminated t:
                _log.Warning("FTX Feed Reader unexpectedly terminated. Restarting...");
                BecomeRestarting();
                break;
            case GetReaderReference r:
                Sender.Tell(_feedReader);
                break;
            default:
                return false;
        }

        return true;
    }

    private void UpdateHeartbeat()
    {
        _lastHeartbeat = DateTime.UtcNow; // update the latest heartbeat interval
    }

    private void BecomeRestarting(bool terminated = true)
    {
        if (terminated) // the FTX Feed Publisher has already been terminated
        {
            _feedReader = SpawnFtxFeedReader(); // create a new reader
            Stash.UnstashAll();
            Become(Connecting);
        }
        else // need to go through a restart behavior first
        {
            Context.Stop(_feedReader);
            Become(WaitingForTermination);
        }

        // terminate heartbeats
        _heartbeatCheck?.Cancel();

        // terminate ping sending
        _sendPing?.Cancel();
    }

    private void DeliverBuffer()
    {
        unchecked
        {
            var totalDemand = (int)TotalDemand;
            foreach (var b in _messages.Take(totalDemand).ToList())
            {
                OnNext(b);
                _messages.RemoveFirst();
            }
        }
    }

    private IActorRef SpawnFtxFeedReader()
    {
        var reader = Context.ActorOf(Props.Create(() =>
            new FtxFeedReader(_settings, Self)), "reader");
        Context.Watch(reader);
        return reader;
    }

    /// <summary>
    ///     Acts as a completion signal for gracefully terminating the FTX feed in the event that it needs to migrate.
    /// </summary>
    public sealed class Complete
    {
        public static readonly Complete Instance = new();

        private Complete()
        {
        }
    }

    /// <summary>
    ///     INTERNAL API.
    ///     Blows up the socket connection on purpose. Used for testing.
    /// </summary>
    public sealed class ForciblyClose
    {
        public static readonly ForciblyClose Instance = new();

        private ForciblyClose()
        {
        }
    }

    /// <summary>
    ///     Represents data gathered from the publisher
    /// </summary>
    internal sealed class SocketData
    {
        public SocketData(string data)
        {
            Data = data;
        }

        public string Data { get; }
    }

    internal sealed class CheckHeartbeat
    {
        public static readonly CheckHeartbeat Instance = new();

        private CheckHeartbeat()
        {
        }
    }

    internal sealed class SendPing
    {
        public static readonly SendPing Instance = new();

        private SendPing()
        {
        }
    }

    /// <summary>
    ///     INTERNAL API. Used to abort a connection.
    /// </summary>
    internal sealed class ConnectTimeout
    {
        public static readonly ConnectTimeout Instance = new();

        private ConnectTimeout()
        {
        }
    }

    /// <summary>
    ///     INTERNAL API. Used for acquiring a reference to the <see cref="FtxFeedReader" />
    /// </summary>
    internal sealed class GetReaderReference
    {
        public static readonly GetReaderReference Instance = new();

        private GetReaderReference()
        {
        }
    }
}
