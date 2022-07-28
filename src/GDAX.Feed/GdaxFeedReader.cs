using System.Diagnostics.Contracts;
using System.Net.WebSockets;
using System.Text;
using Akka.Actor;
using Akka.Event;
using GDAX.Client;

namespace GDAX.Feed;

/// <summary>
///     INTERNAL API.
///     Responsible for pulling the messages out of the web socket
/// </summary>
public sealed class GdaxFeedReader : UntypedActor, IWithUnboundedStash
{
    public const int MaxMessageSize = 2048; //2kb
    private readonly bool _authenticate;
    private readonly byte[] _buffer;
    private readonly string _channel;
    private readonly string? _market;
    private readonly TimeSpan _connectTimeout;
    private readonly bool _heartbeat;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _publisherRef;
    private readonly GdaxRealtimeFeedClient _realTime;
    private readonly IActorRef _selfClosure;

    private int _currentCount;

    private CancellationTokenSource _readToken;
    private WebSocket _socket;

    public GdaxFeedReader(IFeedPublisherSettings settings, IActorRef publisherRef)
    {
        Contract.Requires(settings.Client != null);
        Contract.Assert(!string.IsNullOrWhiteSpace(settings.Channel));
        Contract.Assert(settings.BufferSize >= 2048, "bufferSize > 2048 buffer must be at least 2kb in size.");
        _realTime = settings.Client;
        _connectTimeout = settings.ConnectTimeout;
        _authenticate = settings.AuthenticateFeed;
        _heartbeat = settings.TrackHeartbeats;
        _channel = settings.Channel;
        _market = settings.Market;
        _publisherRef = publisherRef;
        _buffer = new byte[settings.BufferSize];
        Become(Connecting);
        _selfClosure = Self;
    }

    public IStash Stash { get; set; }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case WebSocketReceiveResult r:
                var readResult = CompletedRead(r);
                if (readResult.readOp == ReadResult.Close)
                {
                    _socket.CloseAsync(r.CloseStatus.Value, "closing per server request", CancellationToken.None);
                    throw new GdaxException("socket closed unexpectedly");
                }

                _publisherRef.Tell(new GdaxFeedPublisher.SocketData(readResult.outputStr));

                // start another read
                try
                {
                    // dispose the old instance
                    _readToken?.Dispose();
                }
                catch { }

                _readToken = BeginRead(_currentCount);
                break;
            case ReadTimeOut t: // read timeout
                _log.Warning("Socket read timed out. Retrying...");
                try
                {
                    // dispose the old instance
                    _readToken?.Dispose();
                }
                catch { }

                _readToken = BeginRead(_currentCount);
                break;
            case Status.Failure failure: // we had a socket failure
                _log.Error(failure.Cause, "socket read failed. Retrying connection.");
                throw failure.Cause == null
                    ? new GdaxException("received socket failure on read")
                    : new GdaxException("received socket failure on read", failure.Cause);
            case GdaxFeedPublisher.ForciblyClose close:
                // we expect to be stopped and recreated by our parent under the circumstances
                throw new GdaxException("forcibly closing connection.");
            default:
                Unhandled(message);
                break;
        }
    }

    private void Connecting(object message)
    {
        switch (message)
        {
            case SocketOpen s:
                _socket = s.Socket;
                _realTime.Subscribe(_channel, _market, _socket, new CancellationToken(), _authenticate, _heartbeat)
                    .ContinueWith<object>(tr =>
                    {
                        if (tr.IsCanceled)
                        {
                            return new SubscribeFailed(
                                $"Subscription to price feed for channel [{string.Join(",", _channel)}] timed out",
                                tr.Exception);
                        }

                        if (tr.IsFaulted)
                        {
                            return new SubscribeFailed(
                                $"Failed to subscribe to price feed for channel [{string.Join(",", _channel)}]",
                                tr.Exception);
                        }

                        return Subscribed.Instance;
                    }).PipeTo(Self);
                Become(Subscribing);
                break;
            case SocketFailedToOpen fail:
                _log.Error(fail.Ex, fail.Message);
                throw fail.Ex;
            default:
                Stash.Stash(); // stash any other messages
                break;
        }
    }

    private void Subscribing(object message)
    {
        switch (message)
        {
            case SubscribeFailed fail:
                _log.Error(fail.Ex, fail.Message);
                throw fail.Ex;
            case Subscribed s:
                _log.Info("Successfully connected to price feed for channel [{0}]", string.Join(",", _channel));
                Become(OnReceive);
                Stash.UnstashAll();
                _readToken = BeginRead();

                // begin heartbeats to publisher
                _publisherRef.Tell(s);
                break;
            default:
                Stash.Stash();
                break;
        }
    }

    protected override void PreStart()
    {
        /*
         * Initiate connection to client
         */
        StartConnection();
    }

    protected override void PostStop()
    {
        try
        {
            if (_socket?.CloseStatus.HasValue ?? false)
            {
                _log.Info("Closing socket connection to exchange on shutdown...");
                _socket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "terminating",
                    new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token).Wait();
            }
        }
        catch
        {
            // suppress
        }
        finally
        {
            try
            {
                _socket?.Dispose();
            }
            catch
            {
                // suppress
            }
        }
    }

    private void StartConnection()
    {
        _realTime.Connect(_connectTimeout).ContinueWith<object>(tr =>
        {
            if (tr.IsCanceled)
            {
                return new SocketFailedToOpen(
                    $"Connect operation to [{_realTime.Endpoints.WebsocketFeedUri}] timed out",
                    tr.Exception);
            }

            if (tr.IsFaulted)
            {
                return new SocketFailedToOpen(
                    $"Connect operation to [{_realTime.Endpoints.WebsocketFeedUri}] failed due to: {tr.Exception.Message}",
                    tr.Exception);
            }

            return new SocketOpen(tr.Result);
        }).PipeTo(Self);
    }

    private (ReadResult readOp, string outputStr) CompletedRead(WebSocketReceiveResult result)
    {
        if (result.MessageType == WebSocketMessageType.Close)
        {
            _log.Info("pricing feed [{0}] has terminated. Reconnecting...", _realTime.Endpoints.WebsocketFeedUri);
            _socket.CloseAsync(result.CloseStatus.Value, "you closed first, man",
                CancellationToken.None);
            // reconnect logic
            return (ReadResult.Close, string.Empty);
        }

        _currentCount += result.Count;
        if (_currentCount >= _buffer.Length) // check to see if the payload is too large
        {
            ShutdownIfPayloadTooLarge(_currentCount);
        }

        if (result.EndOfMessage) // finished reading the message
        {
            var message = Encoding.UTF8.GetString(_buffer, 0, _currentCount);
            _currentCount = 0; // reset the count
            return (ReadResult.Complete, message);
        }

        // otherwise, defaults to a partial read
        return (ReadResult.Partial, string.Empty);
    }

    private CancellationTokenSource BeginRead(int index = 0)
    {
        if (index + MaxMessageSize > _buffer.Length)
        {
            ShutdownIfPayloadTooLarge(index + MaxMessageSize);
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        if (_socket.State == WebSocketState.Open)
        {
            _socket.ReceiveAsync(new ArraySegment<byte>(_buffer, index, MaxMessageSize), cts.Token)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled) // read timed out
                    {
                        _selfClosure.Tell(ReadTimeOut.Instance);
                    }
                    else if (task.IsFaulted)
                    {
                        _selfClosure.Tell(new Status.Failure(task.Exception));
                    }
                    else
                    {
                        _selfClosure.Tell(task.Result);
                    }
                }, cts.Token);
            return cts;
        }

        return cts;
    }

    private void ShutdownIfPayloadTooLarge(int count)
    {
        _socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "message payload too large",
            CancellationToken.None);
        throw new GdaxException(
            $"GDAX sent {count} bytes, which exceeded our buffer size of {_buffer.Length}");
    }

    /// <summary>
    ///     Used when we cancel a read operation and have to restart reading.
    /// </summary>
    private sealed class ReadTimeOut
    {
        public static readonly ReadTimeOut Instance = new();

        private ReadTimeOut()
        {
        }
    }

    /// <summary>
    ///     Used to signal that we've been able to successfully connect the socket
    /// </summary>
    private sealed class SocketOpen
    {
        public SocketOpen(WebSocket socket)
        {
            Socket = socket;
        }

        public WebSocket Socket { get; }
    }

    /// <summary>
    ///     Used to signal that we failed to open a socket
    /// </summary>
    private sealed class SocketFailedToOpen
    {
        public SocketFailedToOpen(string message, Exception ex)
        {
            Message = message;
            Ex = ex;
        }

        public string Message { get; }

        public Exception Ex { get; }
    }

    /// <summary>
    ///     Used to signal that we've successfully subscribed to the order book
    /// </summary>
    internal sealed class Subscribed
    {
        public static readonly Subscribed Instance = new();

        private Subscribed()
        {
        }
    }

    /// <summary>
    ///     Signals that a subscription operation has failed
    /// </summary>
    private sealed class SubscribeFailed
    {
        public SubscribeFailed(string message, Exception ex)
        {
            Message = message;
            Ex = ex;
        }

        public string Message { get; }

        public Exception Ex { get; }
    }


    private enum ReadResult
    {
        Complete,
        Partial,
        Close
    }
}
