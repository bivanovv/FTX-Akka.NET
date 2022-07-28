namespace GDAX.Client.Http;

/// <summary>
///     INTERNAL API.
///     Full list of required HTTP headers used for authentication.
/// </summary>
internal static class GdaxHeaders
{
    public const string CbAccessKey = "CB-ACCESS-KEY";
    public const string CbAccessSign = "CB-ACCESS-SIGN";
    public const string CbAccessTimestamp = "CB-ACCESS-TIMESTAMP";
    public const string CbAccessPassPhrase = "CB-ACCESS-PASSPHRASE";
    public const string CbAfter = "CB-AFTER";
    public const string CbBefore = "CB-BEFORE";
}
