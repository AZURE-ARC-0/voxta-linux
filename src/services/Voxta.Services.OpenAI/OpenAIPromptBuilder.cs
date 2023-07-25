using System.Text;
using Voxta.Abstractions.Model;
using Microsoft.DeepDev;

namespace Voxta.Services.OpenAI;

public class OpenAIPromptBuilder
{
    private readonly ITokenizer? _tokenizer;

    public OpenAIPromptBuilder(ITokenizer? tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public List<OpenAIMessage> BuildReplyPrompt(IReadOnlyChatSessionData chatSessionData, int maxTokens)
    {
        var systemPrompt = MakeSystemPrompt(chatSessionData.Character);
        var systemPromptTokens = _tokenizer?.Encode(systemPrompt, OpenAISpecialTokens.Keys).Count ?? 0;
        var postHistoryPrompt = MakePostHistoryPrompt(chatSessionData.Character, chatSessionData.Context, chatSessionData.Actions);
        var postHistoryPromptTokens = _tokenizer?.Encode(postHistoryPrompt, OpenAISpecialTokens.Keys).Count ?? 0;

        var totalTokens = systemPromptTokens + postHistoryPromptTokens;
        
        var messages = new List<OpenAIMessage> { new() { role = "system", content = systemPrompt } };
        var chatMessages = chatSessionData.GetMessages();
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            totalTokens += message.Tokens + 4; // https://github.com/openai/openai-python/blob/main/chatml.md
            if (totalTokens >= maxTokens) break;
            var role = message.User == chatSessionData.Character.Name ? "assistant" : "user";
            messages.Insert(1, new() { role = role, content = message.Text });
        }

        if (!string.IsNullOrEmpty(postHistoryPrompt))
            messages.Add(new() { role = "system", content = postHistoryPrompt });

        return messages;
    }

    public List<OpenAIMessage> BuildActionInferencePrompt(ChatSessionData chatSessionData)
    {
        if (chatSessionData.Actions == null || chatSessionData.Actions.Length < 1)
            throw new ArgumentException("No actions provided.", nameof(chatSessionData));
        
        var messages = new List<OpenAIMessage>
        {
            new() {
                role = "system",
                content = $"""
                    You are tasked with inferring the best action from a list based on the content of a sample chat.

                    Actions: {string.Join(", ", chatSessionData.Actions.Select(a => $"[{a}]"))}
                    """
            },
        };
        
        var sb = new StringBuilder();
        sb.AppendLineLinux("Conversation Context:");
        sb.AppendLineLinux(chatSessionData.Character.Name + "'s Personality: " + chatSessionData.Character.Personality);
        sb.AppendLineLinux("Scenario: " + chatSessionData.Character.Scenario);
        if (!string.IsNullOrEmpty(chatSessionData.Context))
            sb.AppendLineLinux($"Context: {chatSessionData.Context}");
        sb.AppendLineLinux();

        sb.AppendLineLinux("Conversation:");
        foreach (var message in chatSessionData.Messages.TakeLast(8))
        {
            sb.AppendLineLinux($"{message.User}: {message.Text}");
        }
        sb.AppendLineLinux();
        sb.AppendLineLinux($"Based on the last message, which of the following actions is the most applicable for {chatSessionData.Character.Name}: {string.Join(", ", chatSessionData.Actions.Select(a => $"[{a}]"))}"); 
        sb.AppendLineLinux();
        sb.AppendLineLinux("Only write the action.");
        
        messages.Add(new OpenAIMessage { role = "user", content = sb.ToString().TrimExcess() });

        return messages;
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
        sb.AppendLineLinux($"Only write a single reply from {character.Name} for natural speech.");
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