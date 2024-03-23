namespace AspNetCore.SignalR.OpenTelemetry;

public interface IHubMetrics
{
    /// <param name="duration">msec</param>
    void CountInvocation(double duration);

    void CountOnConnected();
    void CountOnDisconnected();
}
