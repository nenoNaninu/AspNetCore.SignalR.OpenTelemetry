using Microsoft.AspNetCore.SignalR;

namespace TestApp.AspNetCore.Hubs;

public class UserDefinedType
{
    public DateTime DateTime { get; set; }
    public Guid Guid { get; set; }
}

public interface IUnaryHub
{
    Task<string> Get();
    Task<int> Add(int x, int y);
    Task<string> Cat(string x, string y);
    Task<UserDefinedType> Echo(UserDefinedType instance);
}

public sealed class UnaryHub : Hub, IUnaryHub
{
    private readonly ILogger<UnaryHub> _logger;

    public UnaryHub(ILogger<UnaryHub> logger)
    {
        _logger = logger;
    }

    public Task<int> Add(int x, int y)
    {
        _logger.Log(LogLevel.Information, "UnaryHub.Add");

        return Task.FromResult(x + y);
    }

    public Task<string> Cat(string x, string y)
    {
        _logger.Log(LogLevel.Information, "UnaryHub.Cat");

        return Task.FromResult(x + y);
    }

    public Task<UserDefinedType> Echo(UserDefinedType instance)
    {
        _logger.Log(LogLevel.Information, "UnaryHub.Echo");

        return Task.FromResult(instance);
    }

    public Task<string> Get()
    {
        _logger.Log(LogLevel.Information, "UnaryHub.Get");

        return Task.FromResult("TypedSignalR.Client");
    }
}
