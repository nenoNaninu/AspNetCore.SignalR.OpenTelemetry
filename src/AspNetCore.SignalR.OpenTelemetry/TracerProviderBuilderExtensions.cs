using AspNetCore.SignalR.OpenTelemetry.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Trace;

namespace AspNetCore.SignalR.OpenTelemetry;

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddSignalRInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(HubActivitySource.Name);
    }
}

public static class SignalRServerBuilderExtensions
{
    public static void AddHubInstrumentation(this ISignalRServerBuilder builder)
    {
        builder.Services.TryAddSingleton<HubInstrumentationFilter>();

        builder.Services.PostConfigure<HubOptions>(options =>
        {
            options.AddFilter<HubInstrumentationFilter>();
        });
    }
}
