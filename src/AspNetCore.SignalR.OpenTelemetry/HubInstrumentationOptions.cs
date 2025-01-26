using System;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.SignalR.OpenTelemetry;

public sealed class HubInstrumentationOptions
{
    public Action<Activity, Exception>? OnException { get; set; }

    /// <summary>
    /// Gets or sets a filter function that determines whether or not to collect telemetry on a per invocation basis.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// If filter is <see langword="null" />, all invocations are collected.
    /// </item>
    /// <item>
    /// If filter returns <see langword="true" />, the invocation is collected.
    /// </item>
    /// <item>
    /// If filter returns <see langword="false" /> the invocation is NOT collected.
    /// </item>
    /// </list>
    /// </remarks>
    public Func<HubInvocationContext, bool>? Filter { get; set; }

    public bool UseParentTraceContext { get; set; }
}
