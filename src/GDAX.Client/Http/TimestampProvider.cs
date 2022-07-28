using System.Globalization;

namespace GDAX.Client.Http;

/// <summary>
///     INTERNAL API
///     Used to create ISO 8601-compliant timestamps used by the GDAX API.
/// </summary>
public sealed class TimestampProvider
{
    public static readonly TimestampProvider Default = new();

    private TimestampProvider()
    {
    }

    public string CurrentTimestamp()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", DateTimeFormatInfo.InvariantInfo);
    }

    public double CurrentUnixEpoch()
    {
        return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
