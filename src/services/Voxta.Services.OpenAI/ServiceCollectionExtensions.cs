using Voxta.Abstractions.Services;
using Voxta.Services.OpenAI;
using Microsoft.DeepDev;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddOpenAI(this IServiceCollection services)
    {
        var tokenizer = TokenizerBuilder.CreateByModelName("gpt-3.5-turbo", OpenAISpecialTokens.SpecialTokens);
        services.AddSingleton<ITokenizer>(_ => tokenizer);
        services.AddTransient<OpenAITextGenClient>();
        services.AddTransient<OpenAIActionInferenceClient>();
    }
    
    public static void RegisterOpenAI(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<OpenAITextGenClient>(OpenAIConstants.ServiceName);
    }
    
    public static void RegisterOpenAI(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<OpenAIActionInferenceClient>(OpenAIConstants.ServiceName);
    }
}