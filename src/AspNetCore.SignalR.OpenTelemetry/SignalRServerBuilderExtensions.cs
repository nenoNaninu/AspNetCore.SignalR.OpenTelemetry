using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace AspNetCore.SignalR.OpenTelemetry;

public static class SignalRServerBuilderExtensions
{
    public static ISignalRServerBuilder AddHubInstrumentation(this ISignalRServerBuilder builder)
    {
        return builder.AddHubInstrumentation(_ => { });
    }

    public static ISignalRServerBuilder AddHubInstrumentation(this ISignalRServerBuilder builder, Action<HubInstrumentationOptions> configure)
    {
        builder.Services.TryAddSingleton<HubInstrumentationFilter>();

        builder.Services.PostConfigure<HubOptions>(options =>
        {
            options.AddFilter<HubInstrumentationFilter>();
        });

        builder.Services.Configure(configure);

        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
        {
            // OpenTelemetry.Instrumentation.AspNetCore v1.11.1 introduced EnableAspNetCoreSignalRSupport option. The default is true.
            // This library conflicts with it, so it is set to false.
            // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/releases/tag/Instrumentation.AspNetCore-1.11.1
            options.EnableAspNetCoreSignalRSupport = false;
        });

        return builder;
    }
}
