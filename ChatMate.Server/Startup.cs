﻿using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using ChatMate.Core;
using ChatMate.Data.Yaml;
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
        services.AddWebSockets(options => { });
        
        services.AddHttpClient();
        services.AddScoped<ChatSessionFactory>();
        services.AddSingleton<Sanitizer>();
        services.AddSingleton<PendingSpeechManager>();
        
        services.AddOptions<ProfileSettings>()
            .Bind(_configuration.GetSection("ChatMate.Profile"))
            .ValidateDataAnnotations();
        services.AddYamlRepositories();

        var textGenRegistry = new SelectorRegistry<ITextGenService>();
        services.AddSingleton<ISelectorFactory<ITextGenService>>(sp => new SelectorFactory<ITextGenService>(textGenRegistry, sp));
        var textToSpeechRegistry = new SelectorRegistry<ITextToSpeechService>();
        services.AddSingleton<ISelectorFactory<ITextToSpeechService>>(sp => new SelectorFactory<ITextToSpeechService>(textToSpeechRegistry, sp));
        var animationSelectionRegistry = new SelectorRegistry<IAnimationSelectionService>();
        services.AddSingleton<ISelectorFactory<IAnimationSelectionService>>(sp => new SelectorFactory<IAnimationSelectionService>(animationSelectionRegistry, sp));

        services.AddNovelAI();
        textGenRegistry.RegisterNovelAI();
        textToSpeechRegistry.RegisterNovelAI();

        services.AddOpenAI();
        textGenRegistry.RegisterOpenAI();
        animationSelectionRegistry.RegisterOpenAI();
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