var builder = DistributedApplication.CreateBuilder(args);

builder.AddDotnetProject("server", "../Server/Server.csproj")
    .WithUrlForEndpoint("http", url =>
    {
        url.Url = "/signalr-dev";
        url.DisplayText = "SignalR DevTools";
    });

builder.Build().Run();
