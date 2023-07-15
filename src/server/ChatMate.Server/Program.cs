using ChatMate.Server;

Directory.CreateDirectory("Data");

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://127.0.0.1:5384");

builder.Configuration.AddJsonFile("appsettings.json", optional: true);

var startup = new Startup();
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();
