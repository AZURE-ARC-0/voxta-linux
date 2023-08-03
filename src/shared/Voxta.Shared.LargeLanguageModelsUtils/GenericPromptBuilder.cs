using System.Text;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LargeLanguageModelsUtils;

public class GenericPromptBuilder
{
    private readonly ITokenizer _tokenizer;

    public GenericPromptBuilder(ITokenizer tokenizer)
    {
        _tokenizer = tokenizer;
    }
    
    public string BuildReplyPrompt(IChatInferenceData chat, int maxTokens, bool includePostHistoryPrompt = true)
    {
        var systemPrompt = MakeSystemPrompt(chat);
        var systemPromptTokens = _tokenizer.CountTokens(systemPrompt);
        var postHistoryPrompt = includePostHistoryPrompt ? MakePostHistoryPrompt(chat) : "";
        var postHistoryPromptTokens = _tokenizer.CountTokens(postHistoryPrompt);
        var query = $"{chat.Character.Name}: ";
        var queryTokens = _tokenizer.CountTokens(query);
        var tokens = systemPromptTokens + postHistoryPromptTokens + 1 + queryTokens;
        
        var sb = new StringBuilder();
        var chatMessages = chat.GetMessages();
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            var entry = $"{message.User}: {message.Text}\n";
            var entryTokens = _tokenizer.CountTokens(entry);
            if (tokens + entryTokens >= maxTokens) break;
            sb.Insert(0, entry);
        }

        sb.Insert(0, '\n');
        sb.Insert(0, systemPrompt);

        if (!string.IsNullOrEmpty(postHistoryPrompt))
        {
            sb.AppendLineLinux(postHistoryPrompt);
        }

        sb.Append(query);

        return sb.ToString().TrimExcess();
    }

    public string BuildActionInferencePrompt(IChatInferenceData chatSessionData)
    {
        if (chatSessionData.Actions == null || chatSessionData.Actions.Length < 1)
            throw new ArgumentException("No actions provided.", nameof(chatSessionData));
        
        var sb = new StringBuilder();
        sb.AppendLineLinux($"""
            You are tasked with inferring the best action from a list based on the content of a sample chat.

            Actions: {string.Join(", ", chatSessionData.Actions.Select(a => $"[{a}]"))}
            """.ReplaceLineEndings("\n"));

        sb.AppendLineLinux("Conversation Context:");
        sb.AppendLineLinux(chatSessionData.Character.Name + "'s Personality: " + chatSessionData.Character.Personality);
        sb.AppendLineLinux("Scenario: " + chatSessionData.Character.Scenario);
        if (!string.IsNullOrEmpty(chatSessionData.Context))
            sb.AppendLineLinux($"Context: {chatSessionData.Context}");
        sb.AppendLineLinux();

        sb.AppendLineLinux("Conversation:");
        foreach (var message in chatSessionData.GetMessages().TakeLast(8))
        {
            sb.AppendLineLinux($"{message.User}: {message.Text}");
        }
        sb.AppendLineLinux();
        sb.AppendLineLinux($"Based on the last message, which of the following actions is the most applicable for {chatSessionData.Character.Name}: {string.Join(", ", chatSessionData.Actions.Select(a => $"[{a}]"))}"); 
        sb.AppendLineLinux();
        sb.AppendLineLinux("Only write the action.");
        sb.AppendLineLinux();
        sb.Append("Action: [");
        return sb.ToString().TrimExcess();
    }

    protected virtual string MakeSystemPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(character.SystemPrompt))
            sb.AppendLineLinux(character.SystemPrompt);
        if (!string.IsNullOrEmpty(character.Description))
            sb.AppendLineLinux($"Description of {character.Name}: {character.Description}");
        if (!string.IsNullOrEmpty(character.Personality))
            sb.AppendLineLinux($"Personality of {character.Name}: {character.Personality}");
        if (!string.IsNullOrEmpty(character.Scenario))
            sb.AppendLineLinux($"Circumstances and context of the dialogue: {character.Scenario}");
        if (!string.IsNullOrEmpty(chat.Context))
            sb.AppendLineLinux(chat.Context);
        if (chat.Actions is { Length: > 1 })
            sb.AppendLineLinux($"Potential actions you will be able to do after you respond: {string.Join(", ", chat.Actions)}");
        return sb.ToString().TrimExcess();
    }

    private static string MakePostHistoryPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(character.PostHistoryInstructions))
            sb.AppendLineLinux(string.Join("\n", character.PostHistoryInstructions.Split(new[]{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries).Select(x => $"({x})")));
        return sb.ToString().TrimExcess();
    }
}