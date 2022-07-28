using GDAX.Client.Auth;

namespace GDAX.Client.Http;

/// <summary>
///     INTERNAL API
///     Responsible for applying GDAX auth headers to outbound requests.
/// </summary>
internal sealed class GdaxAuthenticatedRequestHandler : DelegatingHandler
{
    private readonly FtxCredentials _credentials;
    private readonly RequestSigner _signer;
    private readonly TimestampProvider _timestampProvider;

    public GdaxAuthenticatedRequestHandler(FtxCredentials credentials, TimestampProvider timestampProvider)
    {
        _credentials = credentials;
        _timestampProvider = timestampProvider;
        _signer = RequestSigner.FromSecret(credentials.Secret);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);
        ArgumentNullException.ThrowIfNull(request.RequestUri);

        var timestamp = _timestampProvider.CurrentTimestamp();
        request.Headers.Add(GdaxHeaders.CbAccessKey, _credentials.ApiKey);
        request.Headers.Add(GdaxHeaders.CbAccessTimestamp, timestamp);

        var signature = _signer.Sign(timestamp, request.Method.Method, request.RequestUri.PathAndQuery,
            await request.Content.ReadAsStringAsync(cancellationToken));
        request.Headers.Add(GdaxHeaders.CbAccessSign, signature);

        return await base.SendAsync(request, cancellationToken);
    }
}
