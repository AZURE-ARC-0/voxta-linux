using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.Oobabooga;

public class OobaboogaTextGenClient : OobaboogaClientBase, ITextGenService
{
    public string ServiceName => OobaboogaConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    private readonly IPerformanceMetrics _performanceMetrics;

    public OobaboogaTextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics)
    :base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<string> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder();
        // TODO: count tokens?
        var prompt = builder.BuildReplyPrompt(chatSessionData, -1);
        
        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        var text = await SendCompletionRequest(prompt, new[] { "END_OF_DIALOG", "You:", $"{chatSessionData.UserName}:", $"{chatSessionData.Character.Name}:", "\n", "\"" }, cancellationToken);
        textGenPerf.Done();
        
        return text;
    }
}