using ChatMate.Server;
using Serilog;
using Serilog.Events;

using var log = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Default", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware", LogEventLevel.Error)
    .MinimumLevel.Override("Voxta", LogEventLevel.Debug)
    .MinimumLevel.Override("Voxta.Host.AspNetCore.WebSockets", LogEventLevel.Warning)
    .MinimumLevel.Override("Voxta.Server.WebSocketsController", LogEventLevel.Information)
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

Directory.CreateDirectory("Data");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(log);
builder.WebHost.UseUrls("http://127.0.0.1:5384");

builder.Configuration.AddJsonFile("appsettings.json", optional: true);

var startup = new Startup();
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();
