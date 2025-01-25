using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AspNetCore.SignalR.OpenTelemetry.Internal;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCore.SignalR.OpenTelemetry;

public sealed class HubInstrumentationFilter : IHubFilter
{
    private readonly ILogger _logger;
    private readonly HubInstrumentationOptions _options;

    public HubInstrumentationFilter(ILoggerFactory loggerFactory, IOptions<HubInstrumentationOptions> options)
    {
        _logger = loggerFactory.CreateLogger("AspNetCore.SignalR.Logging.HubLoggingFilter");
        _options = options.Value;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (_options.Filter?.Invoke(invocationContext) == false)
        {
            return await next(invocationContext);
        }

        var previousActivity = Activity.Current;

        if (!_options.UseCurrentTraceContext)
        {
            Activity.Current = null;
        }

        var hubName = invocationContext.Hub.GetType().Name;
        var methodName = invocationContext.HubMethodName;
        var connectionId = invocationContext.Context.ConnectionId;
        var address = invocationContext.Context.GetHttpContext()?.Request.Host.Value;

        using var scope = HubLogger.BeginHubMethodInvocationScope(_logger, hubName, methodName);
        using var activity = HubActivitySource.StartInvocationActivity(hubName, methodName, connectionId, address);

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

            InvokeOptionExceptionHandler(activity, exception);

            throw;
        }
        finally
        {
            if (!_options.UseCurrentTraceContext)
            {
                Activity.Current = previousActivity;
            }
        }
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var previousActivity = Activity.Current;

        if (!_options.UseCurrentTraceContext)
        {
            Activity.Current = null;
        }

        var hubName = context.Hub.GetType().Name;
        var connectionId = context.Context.ConnectionId;
        var address = context.Context.GetHttpContext()?.Request.Host.Value;

        using var scope = HubLogger.BeginHubMethodInvocationScope(_logger, hubName, nameof(OnConnectedAsync));
        using var activity = HubActivitySource.StartInvocationActivity(hubName, nameof(OnConnectedAsync), connectionId, address);

        try
        {
            var transport = context.Context.Features.Get<IHttpTransportFeature>();
            HubLogger.LogOnConnected(_logger, hubName, transport?.TransportType ?? HttpTransportType.None);

            var stopwatch = ValueStopwatch.StartNew();

            await next(context);

            var duration = stopwatch.GetElapsedTime();

            HubLogger.LogHubMethodInvocationDuration(_logger, duration.TotalMilliseconds);
            HubActivitySource.StopInvocationActivityOk(activity);
        }
        catch (Exception exception)
        {
            HubActivitySource.StopInvocationActivityError(activity, exception);

            InvokeOptionExceptionHandler(activity, exception);

            throw;
        }
        finally
        {
            if (!_options.UseCurrentTraceContext)
            {
                Activity.Current = previousActivity;
            }
        }
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        var previousActivity = Activity.Current;

        if (!_options.UseCurrentTraceContext)
        {
            Activity.Current = null;
        }

        var hubName = context.Hub.GetType().Name;
        var connectionId = context.Context.ConnectionId;
        var address = context.Context.GetHttpContext()?.Request.Host.Value;

        using var scope = HubLogger.BeginHubMethodInvocationScope(_logger, hubName, nameof(OnDisconnectedAsync));
        using var activity = HubActivitySource.StartInvocationActivity(hubName, nameof(OnDisconnectedAsync), connectionId, address);

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
        }
        catch (Exception ex)
        {
            HubActivitySource.StopInvocationActivityError(activity, ex);

            InvokeOptionExceptionHandler(activity, ex);

            throw;
        }
        finally
        {
            if (!_options.UseCurrentTraceContext)
            {
                Activity.Current = previousActivity;
            }
        }
    }

    private void InvokeOptionExceptionHandler(Activity? activity, Exception exception)
    {
        if (_options.OnException is not null && activity is not null && activity.IsAllDataRequested)
        {
            _options.OnException(activity, exception);
        }
    }
}
