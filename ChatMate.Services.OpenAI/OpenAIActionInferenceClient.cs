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

        var builder = new OpenAIPromptBuilder(null);
        var messages = builder.BuildActionInferencePrompt(chatSessionData);
        
        var animation = await SendChatRequestAsync(messages, cancellationToken);
        return animation.Trim('\'', '"', '.', '[', ']', ' ').ToLowerInvariant();
    }
}