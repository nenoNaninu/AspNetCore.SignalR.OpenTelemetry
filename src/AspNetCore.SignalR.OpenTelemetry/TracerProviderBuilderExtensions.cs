using AspNetCore.SignalR.OpenTelemetry.Internal;
using OpenTelemetry.Trace;

namespace AspNetCore.SignalR.OpenTelemetry;

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddSignalRInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(HubActivitySource.Name);
    }
}
