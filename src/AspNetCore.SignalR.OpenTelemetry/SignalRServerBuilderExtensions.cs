using AspNetCore.SignalR.OpenTelemetry.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspNetCore.SignalR.OpenTelemetry;

public static class SignalRServerBuilderExtensions
{
    public static ISignalRServerBuilder AddHubInstrumentation(this ISignalRServerBuilder builder)
    {
        builder.Services.TryAddSingleton<HubInstrumentationFilter>();
        builder.Services.TryAddSingleton<IHubMetrics, HubMetrics>();

        builder.Services.PostConfigure<HubOptions>(options =>
        {
            options.AddFilter<HubInstrumentationFilter>();
        });

        return builder;
    }
}
