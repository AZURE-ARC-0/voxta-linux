using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;
using Voxta.Services.OpenSourceLargeLanguageModels;

namespace Voxta.Services.NovelAI;

public class NovelAITextGenService : NovelAIClientBase, ITextGenService
{
    private readonly IPerformanceMetrics _performanceMetrics;

    public NovelAITextGenService(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider)
        : base(settingsRepository, httpClientFactory, encryptionProvider)
    {
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new NovelAIPromptBuilder(Tokenizer);
        var textGenPerf = _performanceMetrics.Start("NovelAI.TextGen");
        var input = builder.BuildReplyPrompt(chat, MaxContextTokens, includePostHistoryPrompt: false);
        var text = await SendCompletionRequest(input, "special_instruct", cancellationToken);
        textGenPerf.Done();
        return text;
    }
}