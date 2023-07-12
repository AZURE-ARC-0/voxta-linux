using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.DeepDev;

namespace ChatMate.Services.OpenAI;

public class OpenAITextGenClient : OpenAIClientBase, ITextGenService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ITokenizer _tokenizer;
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;

    public OpenAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ITokenizer tokenizer, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
        : base(httpClientFactory)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.TextGen");
        _settingsRepository = settingsRepository;
        _tokenizer = tokenizer;
        _sanitizer = sanitizer;
        _performanceMetrics = performanceMetrics;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<OpenAISettings>(OpenAIConstants.ServiceName, cancellationToken);
        InitializeClient(settings);
    }

    public int GetTokenCount(string message)
    {
        if (string.IsNullOrEmpty(message)) return 0;
        return _tokenizer.Encode(message, OpenAISpecialTokens.Keys).Count;
    }

    public async ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var systemPrompt = MakeSystemPrompt(chatSessionData.Character);
        var postHistoryPrompt = MakePostHistoryPrompt(chatSessionData.Character);
        
        var tokenizePerf = _performanceMetrics.Start("OpenAI.Tokenize");

        #warning Save this
        var totalTokens = _tokenizer.Encode(systemPrompt, OpenAISpecialTokens.Keys).Count + _tokenizer.Encode(postHistoryPrompt, OpenAISpecialTokens.Keys).Count;
        
        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        var chatMessages = chatSessionData.GetMessages();
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            totalTokens += message.Tokens + 4; // https://github.com/openai/openai-python/blob/main/chatml.md
            if (totalTokens >= 4096) break;
            var role = message.User == chatSessionData.Character.Name ? "assistant" : "user";
            messages.Insert(1, new { role, content = message.Text });
        }

        if (!string.IsNullOrEmpty(postHistoryPrompt))
            messages.Add(new { role = "system", content = postHistoryPrompt });

        tokenizePerf.Pause();

        var textGenPerf = _performanceMetrics.Start("OpenAI.TextGen");
        var reply = await SendChatRequestAsync(messages, cancellationToken);
        textGenPerf.Done();
        
        var sanitized = _sanitizer.Sanitize(reply);
        
        tokenizePerf.Resume();
        var tokens = _tokenizer.Encode(sanitized, OpenAISpecialTokens.Keys);
        tokenizePerf.Done();
        
        return new TextData
        {
            Text = sanitized,
            Tokens = tokens.Count
        };
    }

    private static string MakeSystemPrompt(CharacterCard character)
    {
        return $"""
            {character.SystemPrompt}
            Description of {character.Name}: {character.Description}
            Personality of {character.Name}: {character.Personality}
            Circumstances and context of the dialogue: {character.Scenario}
            """;
    }

    private static string MakePostHistoryPrompt(CharacterCard character)
    {
        return character.PostHistoryInstructions ?? "";
    }
}