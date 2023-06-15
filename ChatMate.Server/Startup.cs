using ChatMate.Server;
using ChatMate.Server.Services;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.DeepDev;
using Microsoft.Extensions.Options;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddWebSockets(options => { });
        
        services.AddHttpClient();
        services.AddScoped<ChatSessionFactory>();
        services.AddSingleton<Sanitizer>();
        
        services.Configure<NovelAIOptions>(_configuration.GetSection("ChatMate.Services:NovelAI"));
        services.AddSingleton<NovelAIClient>();
        
        services.Configure<OpenAIOptions>(_configuration.GetSection("ChatMate.Services:OpenAI"));
        services.AddSingleton<OpenAIClient>();

        // Service selection should be dynamic rather than fixed, i.e. multiple users
        
        services.AddSingleton<ITextGenService>(sp =>
        {
            var textGen = _configuration.GetSection("ChatMate.Server")["TextGen"];
            return textGen switch
            {
                "OpenAI" => sp.GetRequiredService<OpenAIClient>(),
                "NovelAI" => sp.GetRequiredService<NovelAIClient>(),
                _ => throw new NotSupportedException($"TextGen not supported: {textGen}")
            };
        });
        
        services.AddSingleton<ITextToSpeechService>(sp =>
        {
            var textGen = _configuration.GetSection("ChatMate.Server")["SpeechGen"];
            return textGen switch
            {
                "NovelAI" => sp.GetRequiredService<NovelAIClient>(),
                _ => throw new NotSupportedException($"TextGen not supported: {textGen}")
            };
        });
        
        services.AddSingleton<IAnimationSelectionService>(sp =>
        {
            var textGen = _configuration.GetSection("ChatMate.Server")["AnimSelect"];
            return textGen switch
            {
                "OpenAI" => sp.GetRequiredService<OpenAIClient>(),
                _ => throw new NotSupportedException($"TextGen not supported: {textGen}")
            };
        });
        
        services.AddSingleton<ITokenizer>(sp => TokenizerBuilder.CreateByModelName(sp.GetRequiredService<IOptions<OpenAIOptions>>().Value.Model, OpenAISpecialTokens.SpecialTokens));
    }

    public void Configure(IApplicationBuilder  app, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseWebSockets();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}