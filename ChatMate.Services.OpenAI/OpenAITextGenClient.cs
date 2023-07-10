using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.DeepDev;

namespace ChatMate.Services.OpenAI;

public static class OpenAISpecialTokens
{
    // ReSharper disable InconsistentNaming
    private const string IM_START = "<|im_start|>";
    private const string IM_END = "<|im_end|>";
    // ReSharper restore InconsistentNaming

    public static readonly Dictionary<string, int> SpecialTokens = new()
    {
        { IM_START, 100264},
        { IM_END, 100265},
    };
    public static readonly HashSet<string> Keys = new(SpecialTokens.Keys);
}

public class OpenAITextGenClient : ITextGenService, IAnimationSelectionService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ITokenizer _tokenizer;
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;
    private string _model = "gpt-3.5-turbo";

    public OpenAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ITokenizer tokenizer, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
    {
        _httpClient = httpClientFactory.CreateClient(OpenAIConstants.ServiceName);
        _settingsRepository = settingsRepository;
        _tokenizer = tokenizer;
        _sanitizer = sanitizer;
        _performanceMetrics = performanceMetrics;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<OpenAISettings>(OpenAIConstants.ServiceName, cancellationToken);
        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        if (string.IsNullOrEmpty(settings?.ApiKey)) throw new AuthenticationException("OpenAI api key is missing.");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",  Crypto.DecryptString(settings.ApiKey));
        _model = settings.Model;
    }

    public int GetTokenCount(string message)
    {
        if (string.IsNullOrEmpty(message)) return 0;
        return _tokenizer.Encode(message, OpenAISpecialTokens.Keys).Count;
    }

    public async ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var tokenizePerf = _performanceMetrics.Start("OpenAI.Tokenize");
        
        var totalTokens = chatSessionData.Preamble.Tokens + 4;
        if (!string.IsNullOrEmpty(chatSessionData.Postamble?.Text))
            totalTokens += chatSessionData.Postamble.Tokens + 4;
        
        var messages = new List<object> { new { role = "system", content = chatSessionData.Preamble.Text } };
        var chatMessages = chatSessionData.GetMessages();
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            totalTokens += message.Tokens + 4; // https://github.com/openai/openai-python/blob/main/chatml.md
            if (totalTokens >= 4096) break;
            var role = message.User == chatSessionData.BotName ? "assistant" : "user";
            messages.Insert(1, new { role, content = message.Text });
        }

        if (!string.IsNullOrEmpty(chatSessionData.Postamble?.Text))
            messages.Add(new { role = "system", content = chatSessionData.Postamble.Text });

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

    public async ValueTask<string> SelectAnimationAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder(chatSessionData.Preamble.Text);
        sb.AppendLine("");
        foreach (var message in chatSessionData.Messages.TakeLast(4))
        {
            sb.AppendLine($"{message.User}: {message.Text}");
        }

        sb.AppendLine($"""
        ---
        Available animations: smile, frown, pensive, excited, sad, curious, afraid, angry, surprised, laugh, cry, idle
        Write the animation {chatSessionData.BotName} should play.
        """);
        var messages = new List<object>
        {
            new { role = "system", content = "You are a tool that selects the character animation for a Virtual Reality game. You will be presented with a chat, and must provide the animation to play from the provided list. Only answer with a single animation name. Example response: smile" },
            new { role = "user", content = sb.ToString() }
        };
        
        var animation = await SendChatRequestAsync(messages, cancellationToken);
        return animation.Trim('\'', '"', '.', '[', ']').ToLowerInvariant();
    }

    private async Task<string> SendChatRequestAsync(List<object> messages, CancellationToken cancellationToken)
    {
        var body = new
        {
            model = _model,
            messages,
            max_tokens = 120,
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new OpenAIException(await response.Content.ReadAsStringAsync(cancellationToken));

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var apiResponse = (JsonElement?)await JsonSerializer.DeserializeAsync<dynamic>(stream, cancellationToken: cancellationToken);

        if (apiResponse == null) throw new NullReferenceException("OpenAI API response was null");

        return apiResponse.Value.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}