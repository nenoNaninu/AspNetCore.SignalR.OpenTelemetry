using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace AspNetCore.SignalR.OpenTelemetry.Internal;

internal readonly struct ValueStopwatch
{
    private readonly long _startTimestamp;

    public bool IsActive => _startTimestamp != 0;

    private ValueStopwatch(long startTimestamp)
    {
        _startTimestamp = startTimestamp;
    }

    public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());

    public TimeSpan GetElapsedTime()
    {
        if (!IsActive)
        {
            ThrowInvalidOperationException();
        }

        var end = Stopwatch.GetTimestamp();

        return Stopwatch.GetElapsedTime(_startTimestamp, end);
    }

    [DoesNotReturn]
    private static void ThrowInvalidOperationException()
    {
        throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");
    }
}
