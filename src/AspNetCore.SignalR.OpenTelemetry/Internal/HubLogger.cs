using System;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Logging;

namespace AspNetCore.SignalR.OpenTelemetry.Internal;

internal static partial class HubLogger
{
    [LoggerMessage(8200, LogLevel.Information, "SignalR connection established to {HubName} over {TransportType}")]
    public static partial void LogOnConnected(ILogger logger, string hubName, HttpTransportType transportType);

    [LoggerMessage(8201, LogLevel.Information, "Invoking the SignalR hub method {HubName}.{MethodName}")]
    public static partial void LogHubMethodInvocation(ILogger logger, string hubName, string methodName);

    [LoggerMessage(8202, LogLevel.Information, "Duration: {Duration}ms")]
    public static partial void LogHubMethodInvocationDuration(ILogger logger, double duration);

    [LoggerMessage(8208, LogLevel.Information, "SignalR connection to {HubName} was disconnected")]
    public static partial void LogOnDisconnected(ILogger logger, string hubName);

    [LoggerMessage(8209, LogLevel.Information, "SignalR connection to {HubName} was disconnected with exception")]
    public static partial void LogOnDisconnectedWithError(ILogger logger, string hubName, Exception exception);

    private static readonly Func<ILogger, string, string, Guid, IDisposable?> BeginHubMethodInvocationScopeCallback
        = LoggerMessage.DefineScope<string, string, Guid>("HubName:{HubName}, MethodName:{MethodName}, InvocationId:{InvocationId}");

    public static IDisposable? BeginHubMethodInvocationScope(ILogger logger, string hubName, string methodName)
    {
        return BeginHubMethodInvocationScopeCallback(logger, hubName, methodName, Guid.NewGuid());
    }
}
