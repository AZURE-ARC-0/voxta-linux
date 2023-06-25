﻿using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Services.OpenAI;
using Microsoft.DeepDev;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Data.Yaml;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAI(this IServiceCollection services)
    {
        var tokenizer = TokenizerBuilder.CreateByModelName("gpt-3.5-turbo", OpenAISpecialTokens.SpecialTokens);
        services.AddSingleton<ITokenizer>(_ => tokenizer);
        services.AddSingleton<OpenAIClient>();
        return services;
    }
    
    public static void RegisterOpenAI(this ISelectorRegistry<ITextGenService> registry)
    {
        registry.Add<OpenAIClient>("OpenAI");
    }
    
    public static void RegisterOpenAI(this ISelectorRegistry<IAnimationSelectionService> registry)
    {
        registry.Add<OpenAIClient>("OpenAI");
    }
}