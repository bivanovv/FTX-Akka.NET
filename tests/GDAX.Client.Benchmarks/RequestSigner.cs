using System.Security.Cryptography;
using System.Text;

namespace GDAX.Client.Benchmarks;

/// <summary>
///     Used for generating valid signatures for requests
/// </summary>
public sealed class RequestSigner
{
    private readonly byte[] _base64Secret;

    public RequestSigner(byte[] base64Secret)
    {
        _base64Secret = base64Secret;
    }

    /// <summary>
    ///     Creates a new <see cref="RequestSigner" /> from the raw secret,
    ///     without any base64 encoding.
    /// </summary>
    /// <param name="secret">The GDAX API secret.</param>
    /// <returns>A new <see cref="RequestSigner" /> instance.</returns>
    public static RequestSigner FromSecret(string secret)
    {
        return new RequestSigner(Convert.FromBase64String(secret));
    }

    /// <summary>
    ///     Creates a base64 + HMACSHA256 signature used for authorization
    ///     with the
    /// </summary>
    /// <param name="timestamp">The timestamp used by the CB-TIMESTAMP header</param>
    /// <param name="method">The HTTP method used.</param>
    /// <param name="requestPath">The path of the request against the GDAX api.</param>
    /// <param name="body">The content body.</param>
    /// <returns>A signed hash of the above content plus API secret.</returns>
    public string Sign(string timestamp, string method, string requestPath, string body)
    {
        var orig = Encoding.UTF8.GetBytes(timestamp + method.ToUpperInvariant() + requestPath + body);
        using var hmac = new HMACSHA256(_base64Secret);
        return Convert.ToBase64String(hmac.ComputeHash(orig));
    }
}
