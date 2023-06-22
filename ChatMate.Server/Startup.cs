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
        services.AddSingleton<PendingSpeechManager>();
        
        services.Configure<ProfileOptions>(_configuration.GetSection("ChatMate.Profile"));
        services.AddSingleton<IBotRepository, BotYamlFileRepository>();

        var textGenRegistry = new SelectorRegistry<ITextGenService>();
        services.AddSingleton<SelectorFactory<ITextGenService>>(sp => new SelectorFactory<ITextGenService>(textGenRegistry, sp));
        var textToSpeechRegistry = new SelectorRegistry<ITextToSpeechService>();
        services.AddSingleton<SelectorFactory<ITextToSpeechService>>(sp => new SelectorFactory<ITextToSpeechService>(textToSpeechRegistry, sp));
        var animationSelectionRegistry = new SelectorRegistry<IAnimationSelectionService>();
        services.AddSingleton<SelectorFactory<IAnimationSelectionService>>(sp => new SelectorFactory<IAnimationSelectionService>(animationSelectionRegistry, sp));

        {
            services.Configure<NovelAIOptions>(_configuration.GetSection("ChatMate.Services:NovelAI"));
            services.AddSingleton<NovelAIClient>();
            textGenRegistry.Add<NovelAIClient>("NovelAI");
            textToSpeechRegistry.Add<NovelAIClient>("NovelAI");
        }
        
        {
            services.Configure<OpenAIOptions>(_configuration.GetSection("ChatMate.Services:OpenAI"));
            services.AddSingleton<OpenAIClient>();
            textGenRegistry.Add<OpenAIClient>("OpenAI");
            animationSelectionRegistry.Add<OpenAIClient>("OpenAI");
        }

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