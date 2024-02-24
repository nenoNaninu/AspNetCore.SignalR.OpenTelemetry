using AspNetCore.SignalR.OpenTelemetry.Internal;
using OpenTelemetry.Metrics;

namespace AspNetCore.SignalR.OpenTelemetry;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddSignalRInstrumentation(this MeterProviderBuilder builder)
    {
        return builder.AddMeter(HubMetrics.Name);
    }
}
