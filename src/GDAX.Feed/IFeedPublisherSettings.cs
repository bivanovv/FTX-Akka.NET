namespace GDAX.Feed;

/// <summary>
///     The settings used by the <see cref="GdaxFeedPublisher" />
/// </summary>
public interface IFeedPublisherSettings
{
    GdaxRealtimeFeedClient Client { get; set; }

    TimeSpan ConnectTimeout { get; set; }

    string Channel { get; set; }

    string? Market { get; set; }

    bool AuthenticateFeed { get; set; }

    TimeSpan HeartbeatInterval { get; set; }

    bool TrackHeartbeats { get; set; }

    int BufferSize { get; set; }
}

/// <summary>
///     Default feed publisher settings implementation.
/// </summary>
public sealed class DefaultFeedPublisherSettings : IFeedPublisherSettings
{
    public const int DefaultBufferSize = 1024 * 1024 * 4; //4mb
    public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(5); //3.5


    /// <summary>
    ///     The default connection timeout we're willing to tolerate for the GDAX API
    /// </summary>
    public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(3);

    public DefaultFeedPublisherSettings()
    {
        ConnectTimeout = DefaultConnectTimeout;
        HeartbeatInterval = DefaultHeartbeatInterval;
        BufferSize = DefaultBufferSize;
    }

    public GdaxRealtimeFeedClient Client { get; set; }
    public TimeSpan ConnectTimeout { get; set; }
    public string Channel { get; set; }
    public string? Market { get; set; }
    public bool AuthenticateFeed { get; set; }
    public TimeSpan HeartbeatInterval { get; set; }
    public bool TrackHeartbeats { get; set; }
    public int BufferSize { get; set; }
}
