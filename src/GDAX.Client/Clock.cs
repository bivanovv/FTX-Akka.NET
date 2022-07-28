using System.Diagnostics;

namespace GDAX.Client;

/// <summary>
///     INTERNAL API.
///     Used to make it feasible to measure <see cref="Timing" /> instances easily.
/// </summary>
internal static class Clock
{
    private static readonly Stopwatch Stopwatch;

    static Clock()
    {
        Stopwatch = new Stopwatch();
        Stopwatch.Start();
    }

    /// <summary>
    ///     The current time in milliseconds.
    /// </summary>
    public static long CurrentMs => Stopwatch.ElapsedMilliseconds;
}
