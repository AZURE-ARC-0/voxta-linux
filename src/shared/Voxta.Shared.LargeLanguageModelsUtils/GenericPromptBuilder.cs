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

    public string BuildActionInferencePrompt(IChatInferenceData chat)
    {
        if (chat.Actions == null || chat.Actions.Length < 1)
            throw new ArgumentException("No actions provided.", nameof(chat));
        
        var sb = new StringBuilder();
        sb.AppendLineLinux($"""
            You are tasked with inferring the best action from a list based on the content of a sample chat.

            Actions: {string.Join(", ", chat.Actions.Select(a => $"[{a}]"))}
            """.ReplaceLineEndings("\n"));

        sb.AppendLineLinux("Conversation Context:");
        sb.AppendLineLinux(chat.Character.Name + "'s Personality: " + chat.Character.Personality);
        sb.AppendLineLinux("Scenario: " + chat.Character.Scenario);
        if (chat.Context?.HasValue == true)
            sb.AppendLineLinux($"Context: {chat.Context}");
        sb.AppendLineLinux();

        sb.AppendLineLinux("Conversation:");
        foreach (var message in chat.GetMessages().TakeLast(8))
        {
            sb.AppendLineLinux($"{message.User}: {message.Text}");
        }
        sb.AppendLineLinux();
        sb.AppendLineLinux($"Which of the following actions should be executed to match {chat.Character.Name}'s last message?");
        foreach (var action in chat.Actions)
        {
            sb.AppendLineLinux($"- [{action}]");
        }
        sb.AppendLineLinux();
        sb.AppendLineLinux("Only write the action.");
        sb.AppendLineLinux();
        sb.Append("Action: [");
        return sb.ToString().TrimExcess();
    }

    public string[] SummarizationStopTokens => new string[] { "\n\n" }; 

    public string BuildSummarizationPrompt(IChatInferenceData chat)
    {
        var sb = new StringBuilder();
        sb.AppendLineLinux($"""
            You must write facts about {chat.Character.Name} and {chat.User.Name} from their conversation.
            Facts must be short. Be specific. Write in a way that identifies the user associated with the fact. Use words from the conversation when possible.
            Prefer facts about: physical descriptions, emotional state, relationship progression, gender, sexual orientation, preferences, events.
            
            Conversation:
            <START>
            """.ReplaceLineEndings("\n"));
        
        foreach (var message in chat.GetMessages().Take(10))
        {
            sb.AppendLineLinux($"{message.User}: {message.Text}");
        }
        
        sb.AppendLineLinux("<END>");
        sb.AppendLineLinux();
        sb.AppendLineLinux("Facts learned:");
        return sb.ToString();
    }

    protected virtual string MakeSystemPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        var sb = new StringBuilder();
        if (character.SystemPrompt.HasValue)
            sb.AppendLineLinux(character.SystemPrompt.Text);
        else
            sb.AppendLineLinux($"This is a spoken conversation between {chat.User.Name} and {character.Name}. You are playing the role of {character.Name}. The current date and time is {DateTime.Now.ToString("f", chat.CultureInfo)}.  Keep the conversation flowing, actively engage with {chat.User.Name}. Stay in character. Only use spoken words. Avoid making up facts about {chat.User.Name}.");
        
        sb.AppendLineLinux();
        
        if (character.Description.HasValue)
            sb.AppendLineLinux($"Description of {character.Name}: {character.Description}");
        if (character.Personality.HasValue)
            sb.AppendLineLinux($"Personality of {character.Name}: {character.Personality}");
        if (character.Scenario.HasValue)
            sb.AppendLineLinux($"Circumstances and context of the dialogue: {character.Scenario}");
        if (chat.Context?.HasValue == true)
            sb.AppendLineLinux(chat.Context.Text);
        if (chat.Actions is { Length: > 1 })
            sb.AppendLineLinux($"Optional actions {character.Name} can do: {string.Join(", ", chat.Actions.Select(x => $"[{x}]"))}");
        return sb.ToString().TrimExcess();
    }

    private static string MakePostHistoryPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        var sb = new StringBuilder();
        if (character.PostHistoryInstructions.HasValue)
            sb.AppendLineLinux(string.Join("\n", character.PostHistoryInstructions.Text.Split(new[]{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries).Select(x => $"({x})")));
        return sb.ToString().TrimExcess();
    }
}