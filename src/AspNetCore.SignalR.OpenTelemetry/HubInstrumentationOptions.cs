using System;
using System.Diagnostics;

namespace AspNetCore.SignalR.OpenTelemetry;

public sealed class HubInstrumentationOptions
{
    public Action<Activity, Exception>? OnException { get; set; }
}
