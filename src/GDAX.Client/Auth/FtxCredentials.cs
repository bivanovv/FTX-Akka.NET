namespace GDAX.Client.Auth;

/// <summary>
///     The FTX-supplied credentials
/// </summary>
public sealed class FtxCredentials
{
    public FtxCredentials(string apiKey, string secret)
    {
        ApiKey = apiKey;
        Secret = secret;
    }

    public string ApiKey { get; }

    public string Secret { get; }
}
