using System;
using System.Diagnostics;
using System.Reflection;

namespace AspNetCore.SignalR.OpenTelemetry.Internal;

internal static class HubActivitySource
{
    internal const string Name = "AspNetCore.SignalR.OpenTelemetry";

    private static readonly ActivitySource ActivitySource = new(Name, typeof(HubActivitySource).Assembly.GetPackageVersion());

    internal static Activity? StartInvocationActivity(string hubName, string methodName, string? address)
    {
        // https://github.com/open-telemetry/semantic-conventions/blob/v1.24.0/docs/rpc/rpc-spans.md#span-name
        var activity = ActivitySource.CreateActivity($"{hubName}/{methodName}", ActivityKind.Server);

        // Activity.IsAllDataRequested is same as TelemetrySpan.IsRecording in OpenTelemetry API.
        // https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.7.0/src/OpenTelemetry.Api/Trace/TelemetrySpan.cs#L35-L36
        // https://github.com/open-telemetry/opentelemetry-specification/blob/v1.31.0/specification/trace/sdk.md#sampling
        if (activity is null || activity.IsAllDataRequested == false)
        {
            return null;
        }

        // https://github.com/open-telemetry/semantic-conventions/blob/v1.24.0/docs/rpc/rpc-spans.md#common-attributes
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

        // https://github.com/open-telemetry/opentelemetry-specification/blob/v1.32.0/specification/common/mapping-to-non-otlp.md#span-status
        activity.SetTag("otel.status_code", "OK");
    }

    internal static void StopInvocationActivityError(Activity? activity, Exception exception)
    {
        if (activity is null || activity.IsAllDataRequested == false)
        {
            return;
        }

        activity.SetTag("otel.status_code", "ERROR");

        // https://github.com/open-telemetry/semantic-conventions/blob/v1.25.0/docs/exceptions/exceptions-spans.md#attributes
        activity.SetTag("exception.message", exception.Message);
        activity.SetTag("exception.stacktrace", exception.StackTrace);
        activity.SetTag("exception.type", exception.GetType().FullName);
    }
}

file static class AssemblyExtensions
{
    public static string GetPackageVersion(this Assembly assembly)
    {
        return assembly.GetName().Version?.ToString() ?? string.Empty;
    }
}
