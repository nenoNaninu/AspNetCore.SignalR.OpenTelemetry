using System.Diagnostics;
using AspNetCore.SignalR.OpenTelemetry.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using TestApp.AspNetCore.Hubs;
using TypedSignalR.Client;

namespace AspNetCore.SignalR.OpenTelemetry.Tests;

public class HubInstrumentationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HubInstrumentationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TestDefault()
    {
        // Arrange
        var exportedItems = new List<Activity>();

        using var factory = _factory
            .ConfigureFactory(options => { }, exportedItems);

        await using (var connection = factory.CreateHubConnection("/hubs/unaryhub"))
        {
            var hubProxy = connection.CreateHubProxy<IUnaryHub>(TestContext.Current.CancellationToken);

            // Act
            await connection.StartAsync(TestContext.Current.CancellationToken);

            var result = await hubProxy.Add(1, 1);

            await connection.StopAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, result);
        }

        // Expected activity
        // 1. GET /hubs/unaryhub (web socket connection)
        // 2. UnaryHub/OnConnectedAsync
        // 3. UnaryHub/Add
        // 4. UnaryHub/OnDisconnectedAsync
        Assert.True(SpinWait.SpinUntil(() => exportedItems.Count >= 4, TimeSpan.FromSeconds(1)));

        foreach (var activity in exportedItems)
        {
            Assert.DoesNotContain(
                activity.TraceId,
                exportedItems
                    .Where(x => x != activity) // exclude same reference object
                    .Select(x => x.TraceId)
            );
        }
    }

    [Fact]
    public async Task TestUseParentTraceContext()
    {
        // Arrange
        var exportedItems = new List<Activity>();

        using var factory = _factory
            .ConfigureFactory(options =>
            {
                options.UseParentTraceContext = true;
            }, exportedItems);

        await using (var connection = factory.CreateHubConnection("/hubs/unaryhub"))
        {
            var hubProxy = connection.CreateHubProxy<IUnaryHub>(TestContext.Current.CancellationToken);

            // Act
            await connection.StartAsync(TestContext.Current.CancellationToken);

            var result = await hubProxy.Add(1, 1);

            await connection.StopAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, result);
        }

        // Expected activity
        // 1. GET /hubs/unaryhub (web socket connection)
        // 2. UnaryHub/OnConnectedAsync
        // 3. UnaryHub/Add
        // 4. UnaryHub/OnDisconnectedAsync
        Assert.True(SpinWait.SpinUntil(() => exportedItems.Count >= 4, TimeSpan.FromSeconds(1)));

        var websocketActivity = exportedItems.SingleOrDefault(x => x.Source.Name == "Microsoft.AspNetCore");

        Assert.NotNull(websocketActivity);

        var signalrActivities = exportedItems.Where(x => x.Source.Name != "Microsoft.AspNetCore");

        foreach (var activity in signalrActivities)
        {
            Assert.Equal(websocketActivity.TraceId, activity.TraceId);
        }
    }

    [Fact]
    public async Task TestFilter()
    {
        // Arrange
        var exportedItems = new List<Activity>();

        using var factory = _factory
            .ConfigureFactory(options =>
            {
                options.Filter = context => !string.Equals(context.HubMethodName, "Add", StringComparison.OrdinalIgnoreCase);
            }, exportedItems);

        await using (var connection = factory.CreateHubConnection("/hubs/unaryhub"))
        {
            var hubProxy = connection.CreateHubProxy<IUnaryHub>(TestContext.Current.CancellationToken);

            // Act
            await connection.StartAsync(TestContext.Current.CancellationToken);

            var result = await hubProxy.Add(1, 1);

            await connection.StopAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, result);
        }

        // Expected activity
        // 1. GET /hubs/unaryhub (web socket connection)
        // 2. UnaryHub/OnConnectedAsync
        // 3. UnaryHub/OnDisconnectedAsync
        Assert.True(SpinWait.SpinUntil(() => exportedItems.Count >= 3, TimeSpan.FromSeconds(1)));

        Assert.DoesNotContain(exportedItems, x => x.OperationName == "UnaryHub/Add");
    }

    [Fact]
    public async Task TestAttribute()
    {
        // Arrange
        var exportedItems = new List<Activity>();

        string? connectionId = null;

        using var factory = _factory
            .ConfigureFactory(options =>
            {
                options.Filter = context =>
                {
                    if (string.Equals(context.HubMethodName, "Add", StringComparison.OrdinalIgnoreCase))
                    {
                        connectionId = context.Context.ConnectionId;
                    }
                    return true;
                };
            }, exportedItems);

        await using (var connection = factory.CreateHubConnection("/hubs/unaryhub"))
        {
            var hubProxy = connection.CreateHubProxy<IUnaryHub>(TestContext.Current.CancellationToken);

            // Act

            await connection.StartAsync(TestContext.Current.CancellationToken);

            var result = await hubProxy.Add(1, 1);

            await connection.StopAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, result);
        }

        Assert.True(SpinWait.SpinUntil(() => exportedItems.Count >= 4, TimeSpan.FromSeconds(1)));
        Assert.Equal(4, exportedItems.Count);

        foreach (var activity in exportedItems.Where(x => x.Source.Name == HubActivitySource.Name))
        {
            Assert.Contains(activity.Tags, x => x.Key == "signalr.connection.id" && x.Value == connectionId);
        }
    }

    [Fact]
    public async Task TestOnException()
    {
        // Arrange
        var exportedItems = new List<Activity>();

        HubException? exception = null;

        using var factory = _factory
            .ConfigureFactory(options =>
            {
                options.OnException = (activity, ex) =>
                {
                    if (ex is HubException hubException)
                    {
                        exception = hubException;
                    }
                };
            }, exportedItems);

        await using (var connection = factory.CreateHubConnection("/hubs/unaryhub"))
        {
            var hubProxy = connection.CreateHubProxy<IUnaryHub>(TestContext.Current.CancellationToken);

            // Act
            try
            {
                await connection.StartAsync(TestContext.Current.CancellationToken);

                await hubProxy.ThrowHubException(1, 1);

                await connection.StopAsync(TestContext.Current.CancellationToken);
            }
            catch
            {
                // eat exception
            }
        }

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("ThrowHubException", exception.Message);
    }

    [Fact]
    public async Task TestEnrichOnConnected()
    {
        // Arrange
        var exportedItems = new List<Activity>();

        using var factory = _factory
            .ConfigureFactory(options =>
            {
                options.EnrichOnConnected = (activity, context) =>
                {
                    activity.AddTag("EnrichOnConnectedKey", "EnrichOnConnectedValue");
                };
            }, exportedItems);

        await using (var connection = factory.CreateHubConnection("/hubs/unaryhub"))
        {
            var hubProxy = connection.CreateHubProxy<IUnaryHub>(TestContext.Current.CancellationToken);

            // Act
            try
            {
                await connection.StartAsync(TestContext.Current.CancellationToken);

                var result = await hubProxy.Add(1, 1);

                await connection.StopAsync(TestContext.Current.CancellationToken);
            }
            catch
            {
                // eat exception
            }
        }

        // Assert

        Assert.True(SpinWait.SpinUntil(() => exportedItems.Count >= 4, TimeSpan.FromSeconds(1)));

        Assert.Contains(exportedItems,
            x => x.Source.Name == HubActivitySource.Name
                && x.Tags.Any(x => x.Key == "EnrichOnConnectedKey" && x.Value == "EnrichOnConnectedValue")
        );
    }

    [Fact]
    public async Task TestEnrichOnDisconnected()
    {
        // Arrange
        var exportedItems = new List<Activity>();

        using var factory = _factory
            .ConfigureFactory(options =>
            {
                options.EnrichOnDisconnected = (activity, context) =>
                {
                    activity.AddTag("EnrichOnDisconnectedKey", "EnrichOnDisconnectedValue");
                };
            }, exportedItems);

        await using (var connection = factory.CreateHubConnection("/hubs/unaryhub"))
        {
            var hubProxy = connection.CreateHubProxy<IUnaryHub>(TestContext.Current.CancellationToken);

            // Act
            try
            {
                await connection.StartAsync(TestContext.Current.CancellationToken);

                var result = await hubProxy.Add(1, 1);

                await connection.StopAsync(TestContext.Current.CancellationToken);
            }
            catch
            {
                // eat exception
            }
        }

        // Assert

        Assert.True(SpinWait.SpinUntil(() => exportedItems.Count >= 4, TimeSpan.FromSeconds(1)));

        Assert.Contains(exportedItems,
            x => x.Source.Name == HubActivitySource.Name
                && x.Tags.Any(x => x.Key == "EnrichOnDisconnectedKey" && x.Value == "EnrichOnDisconnectedValue")
        );
    }

    [Fact]
    public async Task TestEnrichOnMethodInvoked()
    {
        // Arrange
        var exportedItems = new List<Activity>();

        using var factory = _factory
            .ConfigureFactory(options =>
            {
                options.EnrichOnMethodInvoked = (activity, context) =>
                {
                    activity.AddTag($"{context.HubMethodName}Key", $"{context.HubMethodName}Value");
                };
            }, exportedItems);

        await using (var connection = factory.CreateHubConnection("/hubs/unaryhub"))
        {
            var hubProxy = connection.CreateHubProxy<IUnaryHub>(TestContext.Current.CancellationToken);

            // Act
            try
            {
                await connection.StartAsync(TestContext.Current.CancellationToken);

                var result = await hubProxy.Add(1, 1);

                await connection.StopAsync(TestContext.Current.CancellationToken);
            }
            catch
            {
                // eat exception
            }
        }

        // Assert

        Assert.True(SpinWait.SpinUntil(() => exportedItems.Count >= 4, TimeSpan.FromSeconds(1)));

        Assert.Contains(exportedItems,
            x => x.Source.Name == HubActivitySource.Name
                && x.Tags.Any(x => x.Key == "AddKey" && x.Value == "AddValue")
        );
    }
}

file static class Extensions
{
    public static WebApplicationFactory<Program> ConfigureFactory(
        this WebApplicationFactory<Program> factory,
        Action<HubInstrumentationOptions> configure,
        ICollection<Activity> exportedItems)
    {
        return factory
            .WithWebHostBuilder(webhostBuilder =>
            {
                webhostBuilder.ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                });

                webhostBuilder.ConfigureTestServices(services =>
                {
                    services
                        .AddSignalR()
                        .AddHubInstrumentation(configure);

                    services
                        .AddOpenTelemetry()
                        .WithTracing(builder =>
                        {
                            builder
                                .AddAspNetCoreInstrumentation()
                                .AddSignalRInstrumentation()
                                .AddInMemoryExporter(exportedItems);
                        });
                });
            });
    }

    public static HubConnection CreateHubConnection(this WebApplicationFactory<Program> factory, string path)
    {
        var options = factory.ClientOptions;

        var uri = new Uri(options.BaseAddress, path);

        var webSocket = factory.Server.CreateWebSocketClient();

        var connection = new HubConnectionBuilder()
            .WithUrl(uri, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    var client = await webSocket.ConnectAsync(context.Uri, cancellationToken);
                    return client;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        return connection;
    }
}
