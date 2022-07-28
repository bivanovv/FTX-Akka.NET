using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace GDAX.Client.Benchmarks;

public class RequestSigner2 : IDisposable
{
    private readonly HMACSHA256 _hmac;

    public RequestSigner2(byte[] base64Secret)
    {
        _hmac = new HMACSHA256(base64Secret);
    }

    public static RequestSigner2 FromSecret(string secret)
    {
        return new RequestSigner2(Convert.FromBase64String(secret));
    }

    public string Sign(string timestamp, string method, string requestPath, string body)
    {
        DoDisposeChecks();

        if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(requestPath))
        {
            return string.Empty;
        }

        var orig = Encoding.UTF8.GetBytes(timestamp + method.ToUpperInvariant() + requestPath + body);
        return Convert.ToBase64String(_hmac.ComputeHash(orig));
    }

    #region Disposable

    private bool _isDisposed;

    /// <summary>
    ///     Checks if this object has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DoDisposeChecks()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(RequestSigner2));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Disposes of managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">A value indicating whether or not to dispose of managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _hmac.Dispose();
        }

        _isDisposed = true;
    }

    #endregion
}
