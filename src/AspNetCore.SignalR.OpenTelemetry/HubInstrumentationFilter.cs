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
    private readonly IHubMetrics _hubMetrics;

    public HubInstrumentationFilter(ILoggerFactory loggerFactory, IHubMetrics hubMetrics)
    {
        _logger = loggerFactory.CreateLogger("AspNetCore.SignalR.Logging.HubLoggingFilter");
        _hubMetrics = hubMetrics;
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

            var duration = stopwatch.GetElapsedTime().TotalMilliseconds;

            HubLogger.LogHubMethodInvocationDuration(_logger, duration);
            HubActivitySource.StopInvocationActivityOk(activity);

            _hubMetrics.CountInvocation(duration);

            return result;
        }
        catch (Exception exception)
        {
            HubActivitySource.StopInvocationActivityError(activity, exception);
            throw;
        }
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var hubName = context.Hub.GetType().Name;

        using var scope = HubLogger.BeginHubMethodInvocationScope(_logger, hubName, nameof(OnConnectedAsync));
        using var activity = HubActivitySource.StartInvocationActivity(hubName, nameof(OnConnectedAsync));

        try
        {
            var transport = context.Context.Features.Get<IHttpTransportFeature>();
            HubLogger.LogOnConnected(_logger, hubName, transport?.TransportType ?? HttpTransportType.None);

            var stopwatch = ValueStopwatch.StartNew();

            await next(context);

            var duration = stopwatch.GetElapsedTime();

            HubLogger.LogHubMethodInvocationDuration(_logger, duration.TotalMilliseconds);
            HubActivitySource.StopInvocationActivityOk(activity);

            _hubMetrics.CountOnConnected();
        }
        catch (Exception exception)
        {
            HubActivitySource.StopInvocationActivityError(activity, exception);
            throw;
        }
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        var hubName = context.Hub.GetType().Name;

        using var scope = HubLogger.BeginHubMethodInvocationScope(_logger, hubName, nameof(OnDisconnectedAsync));
        using var activity = HubActivitySource.StartInvocationActivity(hubName, nameof(OnDisconnectedAsync));

        try
        {
            if (exception is null)
            {
                HubLogger.LogOnDisconnected(_logger, hubName);
            }
            else
            {
                HubLogger.LogOnDisconnectedWithError(_logger, hubName, exception);
            }

            var stopwatch = ValueStopwatch.StartNew();

            await next(context, exception);

            var duration = stopwatch.GetElapsedTime();

            HubLogger.LogHubMethodInvocationDuration(_logger, duration.TotalMilliseconds);
            HubActivitySource.StopInvocationActivityOk(activity);

            _hubMetrics.CountOnDisconnected();
        }
        catch (Exception ex)
        {
            HubActivitySource.StopInvocationActivityError(activity, ex);
            throw;
        }
    }
}
