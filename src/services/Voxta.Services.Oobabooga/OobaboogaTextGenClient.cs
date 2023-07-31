using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Voxta.Services.OpenSourceLargeLanguageModels;

namespace Voxta.Services.Oobabooga;

public class OobaboogaTextGenClient : OobaboogaClientBase, ITextGenService
{
    public string ServiceName => OobaboogaConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;

    public OobaboogaTextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
    :base(httpClientFactory, settingsRepository)
    {
        _sanitizer = sanitizer;
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder();
        // TODO: count tokens?
        var prompt = builder.BuildReplyPrompt(chatSessionData, -1);
        
        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        var text = await SendCompletionRequest(prompt, new[] { "END_OF_DIALOG", "You:", $"{chatSessionData.UserName}:", $"{chatSessionData.Character.Name}:", "\n", "\"" }, cancellationToken);
        textGenPerf.Done();
        var sanitized = _sanitizer.Sanitize(text);
        
        return new TextData
        {
            Text = sanitized,
            Tokens = 0,
        };
    }
}