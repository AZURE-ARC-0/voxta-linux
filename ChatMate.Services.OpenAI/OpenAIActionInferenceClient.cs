using System.Text;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;

namespace ChatMate.Services.OpenAI;

public class OpenAIActionInferenceClient : OpenAIClientBase, IActionInferenceService
{
    private readonly ISettingsRepository _settingsRepository;

    public OpenAIActionInferenceClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
        : base(httpClientFactory)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.TextGen");
        _settingsRepository = settingsRepository;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<OpenAISettings>(cancellationToken);
        InitializeClient(settings);
    }

    public async ValueTask<string> SelectActionAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        if(chatSessionData.Actions is null || chatSessionData.Actions.Length == 0)
            return "idle";
        if (chatSessionData.Actions.Length == 1) return chatSessionData.Actions[0];
        
        var sb = new StringBuilder();
        sb.AppendLine(chatSessionData.Character.Name + "'s Personality: " + chatSessionData.Character.Personality);
        sb.AppendLine("Scenario: " + chatSessionData.Character.Scenario);
        foreach (var message in chatSessionData.Messages.TakeLast(4))
        {
            sb.AppendLine($"{message.User}: {message.Text}");
        }

        sb.AppendLine("---");
        if (!string.IsNullOrEmpty(chatSessionData.Context))
            sb.AppendLine($"Context: {chatSessionData.Context}");
        if (chatSessionData.Actions.Length > 1)
            sb.AppendLine($"Available actions: {string.Join(", ", chatSessionData.Actions.Select(a => $"[{a}]"))}");
        sb.AppendLine($"Write the action {chatSessionData.Character.Name} should play.");
        var messages = new List<object>
        {
            new { role = "system", content = "You are a tool that selects the character animation for a Virtual Reality game. You will be presented with a chat, and must provide the animation to play from the provided list. Only answer with a single animation name. Example response: smile" },
            new { role = "user", content = sb.ToString() }
        };
        
        var animation = await SendChatRequestAsync(messages, cancellationToken);
        return animation.Trim('\'', '"', '.', '[', ']', ' ').ToLowerInvariant();
    }
}