using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Data.Yaml;
using ChatMate.Server.Chat;
using Microsoft.AspNetCore.WebSockets;

namespace ChatMate.Server;

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
        services.AddWebSockets(_ => { });
        
        services.AddHttpClient();
        services.AddChatMate();
        services.AddSingleton<IPerformanceMetrics, StaticPerformanceMetrics>();
        services.AddSingleton<TemporaryFileCleanupService>();
        services.AddSingleton<ITemporaryFileCleanup>(sp => sp.GetRequiredService<TemporaryFileCleanupService>());
        services.AddHostedService(sp => sp.GetRequiredService<TemporaryFileCleanupService>());
        
        services.AddOptions<ProfileSettings>()
            .Bind(_configuration.GetSection("ChatMate.Profile"))
            .ValidateDataAnnotations();
        services.AddYamlRepositories();

        var textGenRegistry = services.AddTextGenRegistry();
        var textToSpeechRegistry = services.AddTextToSpeechRegistry();
        var animationSelectionRegistry = services.AddAnimationServiceRegistry();

        services.AddOpenAI();
        textGenRegistry.RegisterOpenAI();
        animationSelectionRegistry.RegisterOpenAI();

        services.AddNovelAI();
        textGenRegistry.RegisterNovelAI();
        textToSpeechRegistry.RegisterNovelAI();
        
        services.AddKoboldAI();
        textGenRegistry.RegisterKoboldAI();
        
        services.AddElevenLabs();
        textToSpeechRegistry.RegisterElevenLabs();
        
        services.AddNAudio();

        services.AddVosk(_configuration.GetSection("Vosk"));
        services.AddHostedService<SpeechRecognitionBackgroundTask>();
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