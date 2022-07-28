using System.Net.Http.Headers;
using System.Reflection;
using GDAX.Client.Auth;

namespace GDAX.Client.Http;

/// <summary>
///     INTERNAL API.
///     Used for constructor authenticated API requests against the Ftx endpoints.
/// </summary>
public sealed class GdaxRequestBuilder
{
    private static readonly ProductInfoHeaderValue ProductHeader =
        new("GDAX.NETCore", typeof(GdaxRequestBuilder).GetTypeInfo().Assembly.GetName().Version!.ToString());

    private readonly FtxCredentials _credentials;

    private readonly FtxEndpoints _endpoints;

    public GdaxRequestBuilder(FtxCredentials credentials, TimestampProvider timestampProvider,
        FtxEndpoints endpoints)
    {
        _credentials = credentials;
        TimestampProvider = timestampProvider;
        _endpoints = endpoints;
        Signer = null; //RequestSigner.FromSecret(credentials.Secret);
    }

    /// <summary>
    ///     Made public so it can be shared with the real-time websocket feed.
    /// </summary>
    public TimestampProvider TimestampProvider { get; }

    /// <summary>
    ///     Made public so it can be shared with the real-time websocket feed.
    /// </summary>
    public RequestSigner Signer { get; }
}
