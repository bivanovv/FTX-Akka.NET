namespace GDAX.Client;

/// <summary>
///     General exception thrown by this driver if an operation fails.
/// </summary>
public class GdaxException : Exception
{
    public GdaxException(string message) : base(message)
    {
    }

    public GdaxException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
