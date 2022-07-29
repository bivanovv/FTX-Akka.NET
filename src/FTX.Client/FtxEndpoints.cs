namespace FTX.Client;

/// <summary>
///     Defines the set of REST API endpoints for the FTX Uri.
/// </summary>
public sealed class FtxEndpoints
{
    /// <summary>
    ///     Live FTX API endpoints.
    /// </summary>
    public static readonly FtxEndpoints Live = new("https://ftx.com", "application/json", "wss://ftx.com/ws/");

    private FtxEndpoints(string restEndpointUri, string contentType, string websocketFeedUri)
    {
        RestEndpointUri = restEndpointUri;
        ContentType = contentType;
        WebsocketFeedUri = websocketFeedUri;
    }

    public string RestEndpointUri { get; }

    public string ContentType { get; }

    public string WebsocketFeedUri { get; }
}
