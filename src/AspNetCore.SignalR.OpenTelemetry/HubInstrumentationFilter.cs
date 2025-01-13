using System;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Threading.Tasks;
using AspNetCore.SignalR.OpenTelemetry.Internal;
using Microsoft.Extensions.Logging;

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
        if (ShouldSkipCreateActivity(invocationContext))
        {
            return await next(invocationContext);
        }
        
        var hubName = invocationContext.Hub.GetType().Name;
        var methodName = invocationContext.HubMethodName;
        var address = invocationContext.Context.GetHttpContext()?.Request.Host.Value;            

        using var scope = HubLogger.BeginHubMethodInvocationScope(_logger, hubName, methodName);
        using var activity = HubActivitySource.StartInvocationActivity(hubName, methodName, address, _options.UnsetParentActivity);

        InvokeEnrichWithRequestHandler(activity, invocationContext);

        try
        {
            HubLogger.LogHubMethodInvocation(_logger, hubName, methodName);

            var stopwatch = Internal.ValueStopwatch.StartNew();

            var result = await next(invocationContext);

            var duration = stopwatch.GetElapsedTime();

            HubLogger.LogHubMethodInvocationDuration(_logger, duration.TotalMilliseconds);

            InvokeEnrichWithResponseHandler(activity, result);

            HubActivitySource.StopInvocationActivityOk(activity);

            return result;
        }
        catch (Exception exception)
        {
            HubActivitySource.StopInvocationActivityError(activity, exception);

            InvokeEnrichWithExceptionHandler(activity, exception);

            throw;
        }
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var hubName = context.Hub.GetType().Name;
        var address = context.Context.GetHttpContext()?.Request.Host.Value;

        using var scope = HubLogger.BeginHubMethodInvocationScope(_logger, hubName, nameof(OnConnectedAsync));
        using var activity = HubActivitySource.StartInvocationActivity(hubName, nameof(OnConnectedAsync), address, _options.UnsetParentActivity);

        try
        {
            var transport = context.Context.Features.Get<IHttpTransportFeature>();
            HubLogger.LogOnConnected(_logger, hubName, transport?.TransportType ?? HttpTransportType.None);

            var stopwatch = Internal.ValueStopwatch.StartNew();

            await next(context);

            var duration = stopwatch.GetElapsedTime();

            HubLogger.LogHubMethodInvocationDuration(_logger, duration.TotalMilliseconds);
            HubActivitySource.StopInvocationActivityOk(activity);
        }
        catch (Exception exception)
        {
            HubActivitySource.StopInvocationActivityError(activity, exception);

            this.InvokeEnrichWithExceptionHandler(activity, exception);

            throw;
        }
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        var hubName = context.Hub.GetType().Name;
        var address = context.Context.GetHttpContext()?.Request.Host.Value;

        using var scope = HubLogger.BeginHubMethodInvocationScope(_logger, hubName, nameof(OnDisconnectedAsync));
        using var activity = HubActivitySource.StartInvocationActivity(hubName, nameof(OnDisconnectedAsync), address, _options.UnsetParentActivity);

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

            var stopwatch = Internal.ValueStopwatch.StartNew();

            await next(context, exception);

            var duration = stopwatch.GetElapsedTime();

            HubLogger.LogHubMethodInvocationDuration(_logger, duration.TotalMilliseconds);
            HubActivitySource.StopInvocationActivityOk(activity);
        }
        catch (Exception ex)
        {
            HubActivitySource.StopInvocationActivityError(activity, ex);

            this.InvokeEnrichWithExceptionHandler(activity, ex);

            throw;
        }
    }

    private void InvokeEnrichWithExceptionHandler(Activity? activity, Exception exception)
    {
        var handler = _options.EnrichWithException;
        if (handler is not null && activity is not null && activity.IsAllDataRequested)
        {
            try
            {
                handler(activity, exception);
            }
            catch (Exception ex)
            {
                HubLogger.EnrichWithExceptionError(_logger, ex);
            }
        }
    }
    
    private void InvokeEnrichWithRequestHandler(Activity? activity, HubInvocationContext invocationContext)
    {
        var handler = _options.EnrichWithRequest;
        if (handler is not null && activity is not null && activity.IsAllDataRequested)
        {
            try
            {
                handler(activity, invocationContext);
            }
            catch (Exception ex)
            {
                HubLogger.EnrichWithRequestError(_logger, ex);
            }
        }
    }
    
    private void InvokeEnrichWithResponseHandler(Activity? activity, object? result)
    {
        var handler = _options.EnrichWithResponse;
        if (handler is not null && activity is not null && activity.IsAllDataRequested)
        {
            try
            {
                handler(activity, result);
            }
            catch (Exception ex)
            {
                HubLogger.EnrichWithResponseError(_logger, ex);
            }
        }
    }

    private bool ShouldSkipCreateActivity(HubInvocationContext invocationContext)
    {
        try
        {
            if (_options.Filter?.Invoke(invocationContext) == false)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            HubLogger.FilterHandlerError(_logger, ex);
            return true;
        }

        return false;
    }
}
