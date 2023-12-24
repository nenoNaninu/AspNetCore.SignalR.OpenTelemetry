using System;
using System.Threading.Tasks;
using AspNetCore.SignalR.OpenTelemetry.Internal;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AspNetCore.SignalR.OpenTelemetry;

public sealed class HubInstrumentationFilter : IHubFilter
{
    private readonly ILogger _logger;

    public HubInstrumentationFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("AspNetCore.SignalR.Logging.HubLoggingFilter");
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var hubName = invocationContext.Hub.GetType().Name;
        var methodName = invocationContext.HubMethodName;

        using var scope = HubLogger.BeginHubMethodInvocationScope(_logger, hubName, methodName);
        using var activity = HubActivitySource.StartInvocationActivity(hubName, methodName);

        try
        {
            HubLogger.LogHubMethodInvocation(_logger, hubName, methodName);

            var stopwatch = ValueStopwatch.StartNew();

            var result = await next(invocationContext);

            var duration = stopwatch.GetElapsedTime();

            HubLogger.LogHubMethodInvocationDuration(_logger, duration.TotalMilliseconds);
            HubActivitySource.StopInvocationActivityOk(activity);

            return result;
        }
        catch (Exception exception)
        {
            HubActivitySource.StopInvocationActivityError(activity, exception);
            throw;
        }
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var hubName = context.Hub.GetType().Name;

        var transport = context.Context.Features.Get<IHttpTransportFeature>();
        HubLogger.LogOnConnected(_logger, hubName, transport?.TransportType ?? HttpTransportType.None);

        return next(context);
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        var hubName = context.Hub.GetType().Name;

        if (exception is null)
        {
            HubLogger.LogOnDisconnected(_logger, hubName);
        }
        else
        {
            HubLogger.LogOnDisconnectedWithError(_logger, hubName, exception);
        }

        return next(context, exception);
    }
}
