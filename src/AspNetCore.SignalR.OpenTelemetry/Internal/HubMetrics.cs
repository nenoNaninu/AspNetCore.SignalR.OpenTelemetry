using System.Diagnostics.Metrics;

namespace AspNetCore.SignalR.OpenTelemetry.Internal;

internal sealed class HubMetrics : IHubMetrics
{
    internal const string Name = "SignalR.Hub";

    private readonly UpDownCounter<int> _activeCounter;
    private readonly Counter<int> _connectedCounter;
    private readonly Counter<int> _disconnectedCounter;

    private readonly Counter<int> _messageCounter;
    private readonly Histogram<double> _durationHistogram;

    public HubMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Name);

        // https://opentelemetry.io/docs/specs/semconv/general/metrics/#use-count-instead-of-pluralization-for-updowncounters
        _activeCounter = meter.CreateUpDownCounter<int>("signalr.connection.active.count");

        _connectedCounter = meter.CreateCounter<int>("signalr.connection.connected");
        _disconnectedCounter = meter.CreateCounter<int>("signalr.connection.disconnected");

        _messageCounter = meter.CreateCounter<int>("signalr.invocation.message");
        _durationHistogram = meter.CreateHistogram<double>("signalr.invocation.duration", "ms");
    }

    /// <param name="duration">msec</param>
    public void CountInvocation(double duration)
    {
        _messageCounter.Add(1);
        _durationHistogram.Record(duration);
    }

    public void CountOnConnected()
    {
        _activeCounter.Add(1);
        _connectedCounter.Add(1);
    }

    public void CountOnDisconnected()
    {
        _activeCounter.Add(-1);
        _disconnectedCounter.Add(1);
    }
}
