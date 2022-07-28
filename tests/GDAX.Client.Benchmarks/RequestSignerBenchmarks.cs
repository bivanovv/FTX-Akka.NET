using System.Globalization;
using BenchmarkDotNet.Attributes;

namespace GDAX.Client.Benchmarks;

[MemoryDiagnoser]
public class RequestSignerBenchmarks
{
    private RequestSigner2 _requestSigner2;

    // Executed only once per a benchmarked method after initialization of benchmark parameters and before all
    // the benchmark method invocations.
    [GlobalSetup]
    public void Setup()
    {
        _requestSigner2 = RequestSigner2.FromSecret("aGlkZGVu");
    }

    // Executed only once per a benchmarked method after all the benchmark method invocations.
    [GlobalCleanup]
    public void Cleanup()
    {
        _requestSigner2.Dispose();
    }

    [Benchmark]
    public void First()
    {
        var timestamp = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
        _requestSigner2.Sign(timestamp, "GET", "products", "");
    }

    [Benchmark]
    public void Second()
    {
        var signer = RequestSigner.FromSecret("aGlkZGVu");

        var timestamp = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
        signer.Sign(timestamp, "GET", "products", "");
    }
}
