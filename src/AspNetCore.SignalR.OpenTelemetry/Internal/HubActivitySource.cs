using System;
using System.Diagnostics;

namespace AspNetCore.SignalR.OpenTelemetry.Internal;

internal static class HubActivitySource
{
    internal const string Name = "SignalR.Hub";

    private static readonly ActivitySource ActivitySource = new(Name);

    internal static Activity? StartInvocationActivity(string hubName, string methodName)
    {
        //https://opentelemetry.io/docs/specs/semconv/rpc/rpc-spans/#span-name
        var activity = ActivitySource.CreateActivity($"{hubName}/{methodName}", ActivityKind.Internal);

        activity?.SetTag("signalr.hub", hubName);
        activity?.SetTag("signalr.method", methodName);

        return activity?.Start();
    }

    internal static void StopInvocationActivityOk(Activity? activity)
    {
        activity?.SetTag("otel.status_code", "OK");
    }

    internal static void StopInvocationActivityError(Activity? activity, Exception exception)
    {
        activity?.SetTag("otel.status_code", "ERROR");
        activity?.SetTag("signalr.hub.exception", exception.ToString());
    }
}
