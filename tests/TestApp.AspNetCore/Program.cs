using AspNetCore.SignalR.OpenTelemetry;
using TestApp.AspNetCore.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.MapHub<UnaryHub>("/hubs/unaryhub");

app.Run();


// for test
public partial class Program;
