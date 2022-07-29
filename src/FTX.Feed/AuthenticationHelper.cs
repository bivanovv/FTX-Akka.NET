using System.Security.Cryptography;
using System.Text;

namespace FTX.Feed;

public static class AuthenticationHelper
{
    public static string Sign(string secretKey, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var resultBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(resultBytes).ToLowerInvariant();
    }
}
