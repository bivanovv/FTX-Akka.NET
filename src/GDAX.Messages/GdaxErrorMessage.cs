namespace GDAX.Messages;

/// <summary>
///     Error message served up via JSON from the GDAX API
/// </summary>
/// <remarks>
///     See https://docs.gdax.com/#requests
/// </remarks>
public sealed class GdaxErrorMessage
{
    public string Message { get; set; }
}
