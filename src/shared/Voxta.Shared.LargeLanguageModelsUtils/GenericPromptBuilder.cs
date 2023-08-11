using System.Globalization;
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
    
    public string BuildReplyPrompt(IChatInferenceData chat, int maxMemoryTokens, int maxTokens, bool includePostHistoryPrompt = true)
    {
        if (maxTokens <= 0) throw new ArgumentException("Max tokens must be larger than zero.", nameof(maxTokens));
        if (maxMemoryTokens >= maxTokens) throw new ArgumentException("Cannot have more memory tokens than the max tokens.", nameof(maxMemoryTokens));
        
        var systemPrompt = MakeSystemPrompt(chat);
        var systemPromptTokens = _tokenizer.CountTokens(systemPrompt);
        /*
        var postHistoryPrompt = includePostHistoryPrompt ? MakePostHistoryPrompt(chat) : "";
        var postHistoryPromptTokens = _tokenizer.CountTokens(postHistoryPrompt);
        */
        var query = $"{chat.Character.Name}: ";
        var queryTokens = _tokenizer.CountTokens(query);
        var totalTokens = systemPromptTokens /*+ postHistoryPromptTokens + 1*/ + queryTokens;
        
        var sb = new StringBuilder();
        
        var memoryTokens = 0;
        if (maxMemoryTokens > 0)
        {
            var memories = chat.GetMemories();
            if (memories.Count > 0)
            {
                sb.AppendLineLinux($"What {chat.Character.Name} knows:");
                foreach (var memory in memories)
                {
                    #warning We should never count tokens here, nor below. Instead keep tokens in the data.
                    var entryTokens = _tokenizer.CountTokens(memory.Text);
                    memoryTokens += entryTokens + 1;
                    if (memoryTokens >= maxMemoryTokens) break;
                    sb.AppendLineLinux(memory.Text);
                }

                totalTokens += memoryTokens;
            }

            memoryTokens++;
            totalTokens++;
            sb.AppendLineLinux();
        }

        var chatMessages = chat.GetMessages();
        var startAtMessage = 0;
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            var entry = $"{message.User}: {message.Text}\n";   
            var entryTokens = _tokenizer.CountTokens(entry);
            if (totalTokens + entryTokens >= maxTokens) break;
            startAtMessage = i;
        }
        
        if (chatMessages.Count - startAtMessage < Math.Min(chatMessages.Count, 4))
            throw new InvalidOperationException($"Reached {maxTokens} before writing at least two message rounds, which will result in incoherent conversations. Either increase max tokens ({totalTokens} / {maxTokens}) and/or reduce memory tokens ({memoryTokens} / {maxMemoryTokens}).");
        
        for (var i = startAtMessage; i < chatMessages.Count; i++)
        {
            var message = chatMessages[i];
            sb.Append(message.User);
            sb.Append(": ");
            sb.AppendLineLinux(message.Text);
        }

        sb.Insert(0, '\n');
        sb.Insert(0, systemPrompt);

        /*
        if (!string.IsNullOrEmpty(postHistoryPrompt))
        {
            sb.AppendLineLinux(postHistoryPrompt);
        }
        */

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
        sb.AppendLineLinux($"Which of the following actions should be executed to match {chatSessionData.Character.Name}'s last message?");
        foreach (var action in chatSessionData.Actions)
        {
            sb.AppendLineLinux($"- [{action}]");
        }
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
        else
            sb.AppendLineLinux($"This is a conversation between {chat.UserName} and {character.Name}. You are playing the role of {character.Name}. The current date and time is {DateTime.Now.ToString("f", CultureInfo.GetCultureInfoByIetfLanguageTag(chat.Character.Culture))}.  Keep the conversation flowing, actively engage with {chat.UserName}. Stay in character.");
        
        sb.AppendLineLinux();
        
        if (!string.IsNullOrEmpty(character.Description))
            sb.AppendLineLinux($"Description of {character.Name}: {character.Description}");
        if (!string.IsNullOrEmpty(character.Personality))
            sb.AppendLineLinux($"Personality of {character.Name}: {character.Personality}");
        if (!string.IsNullOrEmpty(character.Scenario))
            sb.AppendLineLinux($"Circumstances and context of the dialogue: {character.Scenario}");
        if (!string.IsNullOrEmpty(chat.Context))
            sb.AppendLineLinux(chat.Context);
        if (chat.Actions is { Length: > 1 })
            sb.AppendLineLinux($"Optional actions {character.Name} can do: {string.Join(", ", chat.Actions.Select(x => $"[{x}]"))}");
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