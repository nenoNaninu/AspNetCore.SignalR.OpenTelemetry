using System;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Logging;

namespace AspNetCore.SignalR.OpenTelemetry.Internal;

internal static partial class HubLogger
{
    [LoggerMessage(8200, LogLevel.Information, "SignalR connection established to {Hub} over {TransportType}")]
    public static partial void LogOnConnected(ILogger logger, string hub, HttpTransportType transportType);

    [LoggerMessage(8201, LogLevel.Information, "SignalR hub method invocation: {Hub}.{HubMethod}")]
    public static partial void LogHubMethodInvocation(ILogger logger, string hub, string hubMethod);

    [LoggerMessage(8202, LogLevel.Information, "SignalR connection to {Hub} was disconnected")]
    public static partial void LogOnDisconnected(ILogger logger, string hub);

    [LoggerMessage(8203, LogLevel.Information, "SignalR connection to {Hub} was disconnected with exception")]
    public static partial void LogOnDisconnectedWithError(ILogger logger, string hub, Exception exception);

    [LoggerMessage(8204, LogLevel.Information, "Duration: {Duration}ms")]
    public static partial void LogHubMethodInvocationDuration(ILogger logger, double duration);
    
    [LoggerMessage(8205, LogLevel.Error, "Enriching activity with request details threw an error")]
    public static partial void EnrichWithRequestError(ILogger logger, Exception ex);

    [LoggerMessage(8206, LogLevel.Error, "Enriching activity with response details threw an error")]
    public static partial void EnrichWithResponseError(ILogger logger, Exception ex);
    
    [LoggerMessage(8207, LogLevel.Error, "Enriching activity with exception details threw an error")]
    public static partial void EnrichWithExceptionError(ILogger logger, Exception ex);
    
    [LoggerMessage(8208, LogLevel.Error, "Activity filter handler threw an error")]
    public static partial void FilterHandlerError(ILogger logger, Exception ex);
    
    private static readonly Func<ILogger, string, string, Guid, IDisposable?> BeginHubMethodInvocationScopeCallback
        = LoggerMessage.DefineScope<string, string, Guid>("Hub:{Hub}, HubMethod:{HubMethod}, HubInvocationId:{HubInvocationId}");

    public static IDisposable? BeginHubMethodInvocationScope(ILogger logger, string hub, string hubMethod)
    {
        return BeginHubMethodInvocationScopeCallback(logger, hub, hubMethod, Guid.NewGuid());
    }
}
