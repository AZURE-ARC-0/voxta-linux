using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ChatMate.Server.Services;

public class OpenAIOptions
{
    public required string OrganizationId { get; init; }
    public required string ApiKey { get; init; }
    public required string Model { get; init; }
}

public class OpenAIClient : ITextGenService, IAnimationSelectionService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;

    public OpenAIClient(HttpClient httpClient, IOptions<OpenAIOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async ValueTask<string> GenerateReplyAsync(ChatData chatData)
    {
        var messages = new List<object> { new { role = "system", content = chatData.Preamble } };
        foreach (var message in chatData.Messages)
        {
            var role = message.User == chatData.BotName ? "assistant" : "user";
            messages.Add(new { role, content = message.Text });
        }
        return await SendChatRequestAsync(messages);
    }

    public async ValueTask<string> SelectAnimationAsync(ChatData chatData)
    {
        var sb = new StringBuilder(chatData.Preamble);
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