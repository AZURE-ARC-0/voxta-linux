using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.DeepDev;
using Microsoft.Extensions.Options;

namespace ChatMate.Server.Services;

public class OpenAIOptions
{
    [Required, MinLength(40)]
    public required string ApiKey { get; init; }
    
    [Required, MinLength(5)]
    public required string Model { get; init; }
}

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

public class OpenAIClient : ITextGenService, IAnimationSelectionService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenizer _tokenizer;
    private readonly Sanitizer _sanitizer;
    private readonly OpenAIOptions _options;

    public OpenAIClient(HttpClient httpClient, IOptions<OpenAIOptions> options, ITokenizer tokenizer, Sanitizer sanitizer)
    {
        _httpClient = httpClient;
        _tokenizer = tokenizer;
        _sanitizer = sanitizer;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public int GetTokenCount(TextData message)
    {
        return message.Tokens > 0 ? message.Tokens : _tokenizer.Encode(message.Text, OpenAISpecialTokens.Keys).Count;
    }

    public async ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatData chatData)
    {
        var totalTokens = chatData.Preamble.Tokens;
        
        var messages = new List<object> { new { role = "system", content = chatData.Preamble.Text } };
        var chatMessages = chatData.GetMessages();
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            totalTokens += message.Tokens + 4; // https://github.com/openai/openai-python/blob/main/chatml.md
            if (totalTokens >= 4096) break;
            var role = message.User == chatData.BotName ? "assistant" : "user";
            messages.Insert(1, new { role, content = message.Text });
        }

        var reply = await SendChatRequestAsync(messages);
        
        var sanitized = _sanitizer.Sanitize(reply);
        var tokens = _tokenizer.Encode(sanitized, OpenAISpecialTokens.Keys);
        return new TextData
        {
            Text = sanitized,
            Tokens = tokens.Count
        };
    }

    public async ValueTask<string> SelectAnimationAsync(ChatData chatData)
    {
        var sb = new StringBuilder(chatData.Preamble.Text);
        sb.AppendLine();
        foreach (var message in chatData.Messages.TakeLast(4))
        {
            sb.AppendLine($"{message.User}: {message.Text}");
        }

        sb.AppendLine($"""
        ---
        Available animations: smile, frown, pensive, excited, sad, curious, afraid, angry, surprised, laugh, cry, idle
        ---
        Write the animation {chatData.BotName} should play.
        """);
        var messages = new List<object>
        {
            new { role = "system", content = "You are a tool that selects the character animation for a Virtual Reality game. You will be presented with a chat, and must provide the animation that should be used from the provided list. Only answer with a single animation name." },
            new { role = "user", content = sb.ToString() }
        };
        
        var animation = await SendChatRequestAsync(messages);
        return animation.Trim('\'', '"', '.', '[', ']').ToLowerInvariant();
    }

    private async Task<string> SendChatRequestAsync(List<object> messages)
    {
        var body = new
        {
            model = _options.Model,
            messages
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
            throw new OpenAIException(await response.Content.ReadAsStringAsync());

        var apiResponse = (JsonElement?)await JsonSerializer.DeserializeAsync<dynamic>(await response.Content.ReadAsStreamAsync());

        if (apiResponse == null) throw new NullReferenceException("OpenAI API response was null");

        return apiResponse.Value.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}

public class OpenAIException : Exception
{
    public OpenAIException(string message) : base(message)
    {
    }
}