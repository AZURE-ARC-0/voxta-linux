using System.Text;
using ChatMate.Abstractions.Model;

namespace ChatMate.Services.OpenAI;

public class GenericPromptBuilder
{
    public string BuildReplyPrompt(IReadOnlyChatSessionData chatSessionData, int maxTokens)
    {
        var systemPrompt = MakeSystemPrompt(chatSessionData.Character);
        var postHistoryPrompt = MakePostHistoryPrompt(chatSessionData.Character, chatSessionData.Context, chatSessionData.Actions);
        var sb = new StringBuilder();
        var chatMessages = chatSessionData.GetMessages();
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            sb.Insert(0, $"{message.User}: \"{message.Text}\"\n");
        }

        sb.Insert(0, '\n');
        sb.Insert(0, systemPrompt);

        if (!string.IsNullOrEmpty(postHistoryPrompt))
        {
            sb.AppendLineLinux(postHistoryPrompt);
        }

        sb.Append($"{chatSessionData.Character.Name}: \"");

        return sb.ToString().TrimExcess();
    }

    public string BuildActionInferencePrompt(ChatSessionData chatSessionData)
    {
        var sb = new StringBuilder();
        sb.AppendLineLinux("You are a tool that selects the character animation for a Virtual Reality game. You will be presented with a chat, and must provide the animation to play from the provided list. Only answer with a single animation name. Example response: [smile]");
        sb.AppendLineLinux(chatSessionData.Character.Name + "'s Personality: " + chatSessionData.Character.Personality);
        sb.AppendLineLinux("Scenario: " + chatSessionData.Character.Scenario);
        sb.AppendLineLinux("Previous messages:");
        foreach (var message in chatSessionData.Messages.TakeLast(4))
        {
            sb.AppendLineLinux($"{message.User}: {message.Text}");
        }

        sb.AppendLineLinux("---");
        if (!string.IsNullOrEmpty(chatSessionData.Context))
            sb.AppendLineLinux($"Context: {chatSessionData.Context}");
        if (chatSessionData.Actions is { Length: > 1 })
            sb.AppendLineLinux($"Available actions: {string.Join(", ", chatSessionData.Actions.Select(a => $"[{a}]"))}");
        sb.AppendLineLinux($"Write the action {chatSessionData.Character.Name} should play.");
        sb.Append($"Action: [");
        return sb.ToString().TrimExcess();
    }

    private static string MakeSystemPrompt(CharacterCard character)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(character.SystemPrompt))
            sb.AppendLineLinux(character.SystemPrompt);
        if (!string.IsNullOrEmpty(character.Description))
            sb.AppendLineLinux($"Description of {character.Name}: {character.Description}");
        if (!string.IsNullOrEmpty(character.Personality))
            sb.AppendLineLinux($"Personality of {character.Name}: {character.Personality}");
        if (!string.IsNullOrEmpty(character.Scenario))
            sb.AppendLineLinux($"Circumstances and context of the dialogue: {character.Scenario}");
        return sb.ToString().TrimExcess();
    }

    private static string MakePostHistoryPrompt(CharacterCard character, string? context, string[]? actions)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(character.PostHistoryInstructions))
            sb.AppendLineLinux(character.PostHistoryInstructions);
        if (!string.IsNullOrEmpty(context))
            sb.AppendLineLinux($"Current context: {context}");
        if (actions is { Length: > 1 })
            sb.AppendLineLinux($"Available actions to be inferred after the response: {string.Join(", ", actions)}");
        return sb.ToString().TrimExcess();
    }
}