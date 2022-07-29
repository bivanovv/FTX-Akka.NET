namespace FTX.Client;

/// <summary>
///     General exception thrown by this driver if an operation fails.
/// </summary>
public class FtxException : Exception
{
    public FtxException(string message) : base(message)
    {
    }

    public FtxException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
