using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.KoboldAI;

public class KoboldAITextGenService : KoboldAIClientBase, ITextGenService
{
    private readonly IPerformanceMetrics _performanceMetrics;

    public KoboldAITextGenService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics)
        : base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder(Tokenizer);
        var prompt = builder.BuildReplyPrompt(chat, MaxContextTokens);
        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        var text = await SendCompletionRequest(
            prompt,
            new[] { "END_OF_DIALOG", $"{chat.UserName}:", $"{chat.Character.Name}:", "\n" },
            cancellationToken
        );
        textGenPerf.Done();
        return text;
    }
}