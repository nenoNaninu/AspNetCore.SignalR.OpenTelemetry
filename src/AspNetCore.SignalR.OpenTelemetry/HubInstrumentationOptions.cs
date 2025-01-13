using System;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace AspNetCore.SignalR.OpenTelemetry;

public sealed class HubInstrumentationOptions
{
    /// <summary>
    /// Unset the Activity.Current value to prevent grouping all the spans under the same parent
    /// </summary>
    public bool UnsetParentActivity { get; set; }

    /// <summary>
    /// Gets or sets an action to enrich an Activity.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Activity"/>: the activity being enriched.</para>
    /// <para><see cref="Exception"/>: the Exception object from which additional information can be extracted to enrich the activity.</para>
    /// </remarks>
    public Action<Activity, Exception>? EnrichWithException { get; set; }

    /// <summary>
    /// Gets or sets a filter function that determines whether or not to
    /// collect telemetry on a per invocation basis.
    /// </summary>
    /// <remarks>
    /// Notes:
    /// <list type="bullet">
    /// <item>The return value for the filter function is interpreted as:
    /// <list type="bullet">
    /// <item>If filter returns <see langword="true" />, the request is
    /// collected.</item>
    /// <item>If filter returns <see langword="false" /> or throws an
    /// exception the request is NOT collected.</item>
    /// </list></item>
    /// </list>
    /// </remarks>
    public Func<HubInvocationContext, bool>? Filter { get; set; }

    /// <summary>
    /// Gets or sets an action to enrich an Activity.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Activity"/>: the activity being enriched.</para>
    /// <para><see cref="HubInvocationContext"/>: the HubInvocationContext object from which additional information can be extracted to enrich the activity.</para>
    /// </remarks>
    public Action<Activity, HubInvocationContext>? EnrichWithRequest { get; set; }

    /// <summary>
    /// Gets or sets an action to enrich an Activity.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Activity"/>: the activity being enriched.</para>
    /// <para><see cref="object"/>: the object from which additional information can be extracted to enrich the activity.</para>
    /// </remarks>
    public Action<Activity, object?>? EnrichWithResponse { get; set; }
}
