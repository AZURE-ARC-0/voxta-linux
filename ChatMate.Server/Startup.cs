using ChatMate.Server;
using ChatMate.Server.Services;
using Microsoft.AspNetCore.WebSockets;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddWebSockets(options => { });
        
        services.AddHttpClient();
        services.AddSingleton<HttpProxyHandlerFactory>();
        
        services.Configure<NovelAIOptions>(_configuration.GetSection("ChatMate.Services:NovelAI"));
        services.AddSingleton<NovelAIClient>();

        services.AddSingleton<ITextGenService>(sp => sp.GetRequiredService<NovelAIClient>());
        services.AddSingleton<ITextToSpeechService>(sp => sp.GetRequiredService<NovelAIClient>());
    }

    public void Configure(IApplicationBuilder  app, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            // app.UseHsts();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseWebSockets();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
        // app.UseAuthorization();
    }
}