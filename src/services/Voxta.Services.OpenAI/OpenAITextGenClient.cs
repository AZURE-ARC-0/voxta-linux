using Voxta.Abstractions;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;

namespace Voxta.Services.OpenAI;

public class OpenAITextGenClient : OpenAIClientBase, ITextGenService
{   
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public OpenAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider, IServiceObserver serviceObserver)
        : base(httpClientFactory, settingsRepository, encryptionProvider)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.TextGen");
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    protected override async Task<bool> TryInitializeAsync(OpenAISettings settings, IPrerequisitesValidator prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        var success = await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken);
        if (!prerequisites.ValidateFeatures()) return false;
        if (dry || !success) return success;
        if (Parameters == null) throw new NullReferenceException("Parameters should be set at this point");
        var logitBias = (Parameters.LogitBias ??= new Dictionary<string, int>());
        logitBias.TryAdd(string.Join(",", Tokenizer.Tokenize(" safety")), -80);
        logitBias.TryAdd(string.Join(",", Tokenizer.Tokenize(" appropriate")), -80);
        logitBias.TryAdd(string.Join(",", Tokenizer.Tokenize(" AI")), -60);
        return true;
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new OpenAIPromptBuilder(Tokenizer);
        var tokenizePerf = _performanceMetrics.Start("OpenAI.PromptBuilder");
        var messages = builder.BuildReplyPrompt(chat, Settings.MaxMemoryTokens, Settings.MaxContextTokens);
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, OpenAIConstants.ServiceName);
        foreach(var message in messages)
        {
            _serviceObserver.Record($"{ServiceObserverKeys.TextGenPrompt}[{message.Role}]", message.Value);
        }
        tokenizePerf.Pause();

        var textGenPerf = _performanceMetrics.Start("OpenAI.TextGen");
        var body = BuildRequestBody(messages);
        var text = await SendChatRequestAsync(body, cancellationToken);
        textGenPerf.Done();

        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }

    public async ValueTask<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        var messages = new ChatMessageData[]
        {
            new()
            {
                ChatId = Guid.Empty,
                Timestamp = DateTimeOffset.UtcNow,
                Tokens = 0,
                Id = Guid.Empty,
                Role = ChatMessageRole.User,
                Value = prompt,
            }
        };
        var body = BuildRequestBody(messages);
        return await SendChatRequestAsync(body, cancellationToken);
    }
}