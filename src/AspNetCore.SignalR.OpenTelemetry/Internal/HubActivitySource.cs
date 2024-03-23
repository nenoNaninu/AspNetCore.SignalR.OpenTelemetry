using System;
using System.Diagnostics;

namespace AspNetCore.SignalR.OpenTelemetry.Internal;

internal static class HubActivitySource
{
    internal const string Name = "SignalR.Hub";

    private static readonly ActivitySource ActivitySource = new(Name);

    internal static Activity? StartInvocationActivity(string hubName, string methodName, string? address)
    {
        // https://opentelemetry.io/docs/specs/semconv/rpc/rpc-spans/#span-name
        var activity = ActivitySource.CreateActivity($"{hubName}/{methodName}", ActivityKind.Server);

        // Activity.IsAllDataRequested is same as TelemetrySpan.IsRecording in OpenTelemetry API.
        // https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.7.0/src/OpenTelemetry.Api/Trace/TelemetrySpan.cs#L35-L36
        // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/sdk.md#sampling
        if (activity is null || activity.IsAllDataRequested == false)
        {
            return null;
        }

        // https://opentelemetry.io/docs/specs/semconv/rpc/rpc-spans/#common-attributes
        activity.SetTag("rpc.system", "signalr");
        activity.SetTag("rpc.service", hubName);
        activity.SetTag("rpc.method", methodName);

        if (!string.IsNullOrEmpty(address))
        {
            activity.SetTag("server.address", address);
        }

        return activity.Start();
    }

    internal static void StopInvocationActivityOk(Activity? activity)
    {
        if (activity is null || activity.IsAllDataRequested == false)
        {
            return;
        }

        activity.SetTag("otel.status_code", "OK");
    }

    internal static void StopInvocationActivityError(Activity? activity, Exception exception)
    {
        if (activity is null || activity.IsAllDataRequested == false)
        {
            return;
        }

        activity.SetTag("otel.status_code", "ERROR");
        activity.SetTag("signalr.hub.exception", exception.ToString());
    }
}
