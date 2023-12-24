using AspNetCore.SignalR.OpenTelemetry;
using Microsoft.AspNetCore.SignalR;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Server.Hubs;
using TypedSignalR.Client.DevTools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
});

builder.Services.AddControllers();

builder.Services.AddSignalR()
    .AddHubInstrumentation();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(builder =>
    {
        builder.AddService("AspNetCore.SignalR.OpenTelemetry.Example");
    })
    .WithTracing(providerBuilder =>
    {
        providerBuilder
            .AddAspNetCoreInstrumentation()
            .AddSignalRInstrumentation()
            .AddOtlpExporter();
    });


builder.Services.AddSingleton<IMessageRepository, MessageRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSignalRHubSpecification();
    app.UseSignalRHubDevelopmentUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/ChatHub");

app.MapControllers();

app.Run();
