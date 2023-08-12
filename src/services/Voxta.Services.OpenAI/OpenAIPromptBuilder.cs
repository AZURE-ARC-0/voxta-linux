using System.Text;
using Voxta.Abstractions.Model;
using Microsoft.DeepDev;

namespace Voxta.Services.OpenAI;

public class OpenAIPromptBuilder
{
    private readonly ITokenizer _tokenizer;

    public OpenAIPromptBuilder(ITokenizer tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public List<OpenAIMessage> BuildReplyPrompt(IChatInferenceData chat, int maxMemoryTokens, int maxTokens)
    {
        var systemPrompt = MakeSystemPrompt(chat);
        var systemPromptTokens = _tokenizer.Encode(systemPrompt, OpenAISpecialTokens.Keys).Count;
        var postHistoryPrompt = MakePostHistoryPrompt(chat);
        var postHistoryPromptTokens = _tokenizer.Encode(postHistoryPrompt, OpenAISpecialTokens.Keys).Count;

        var totalTokens = systemPromptTokens + postHistoryPromptTokens;

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
                    var entryTokens = _tokenizer.Encode(memory.Text, OpenAISpecialTokens.Keys).Count;
                    memoryTokens += entryTokens + 1;
                    if (memoryTokens >= maxMemoryTokens) break;
                    sb.AppendLineLinux(memory.Text);
                }

                totalTokens += memoryTokens;
            }
        }

        if (memoryTokens > 0)
        {
            systemPrompt += "\n" + sb.ToString().TrimEnd('\n');
        }
        
        var messages = new List<OpenAIMessage> { new() { role = "system", content = systemPrompt } };
        var chatMessages = chat.GetMessages();
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            totalTokens += message.Tokens + 4; // https://github.com/openai/openai-python/blob/main/chatml.md
            if (totalTokens >= maxTokens) break;
            var role = message.User == chat.Character.Name.Text ? "assistant" : "user";
            messages.Insert(1, new() { role = role, content = message.Text });
        }

        if (!string.IsNullOrEmpty(postHistoryPrompt))
            messages.Add(new() { role = "system", content = postHistoryPrompt });

        return messages;
    }

    public List<OpenAIMessage> BuildActionInferencePrompt(IChatInferenceData chat)
    {
        if (chat.Actions == null || chat.Actions.Length < 1)
            throw new ArgumentException("No actions provided.", nameof(chat));
        
        var messages = new List<OpenAIMessage>
        {
            new() {
                role = "system",
                content = $"""
                    You are tasked with inferring the best action from a list based on the content of a sample chat.

                    Actions: {string.Join(", ", chat.Actions.Select(a => $"[{a}]"))}
                    """.ReplaceLineEndings("\n")
            },
        };
        
        var sb = new StringBuilder();
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
        
        messages.Add(new OpenAIMessage { role = "user", content = sb.ToString().TrimExcess() });

        return messages;
    }

    public List<OpenAIMessage> BuildSummarizationPrompt(IChatInferenceData chat)
    {
        var messages = new List<OpenAIMessage>
        {
            new() {
                role = "system",
                content = """
                  You are tasked with extracting knowledge from a conversation for reminiscing.
                  """.ReplaceLineEndings("\n")
            },
        };
        
        var sb = new StringBuilder();
        sb.AppendLineLinux($"""
            Memorize new knowledge {chat.Character.Name} learned in the conversation.

            Use as few words as possible.
            Write from the point of view of {chat.Character.Name}.
            Use telegraphic dense notes style.
            Name the person associated with the memory.
            Only write useful and high confidence memories.
            These categories are the most useful: physical descriptions, emotional state, relationship progression, gender, sexual orientation, preferences, events, state of the participants.

            <START>

            """.ReplaceLineEndings("\n"));
        
        foreach (var message in chat.GetMessages().TakeLast(8))
        {
            sb.AppendLineLinux($"{message.User}: {message.Text}");
        }
        
        sb.AppendLineLinux();
        sb.AppendLineLinux($"What {chat.Character.Name} learned:");
        
        messages.Add(new OpenAIMessage { role = "user", content = sb.ToString().TrimExcess() });

        return messages;
    }

    private static string MakeSystemPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        var sb = new StringBuilder();
        if (character.SystemPrompt.HasValue)
            sb.AppendLineLinux(character.SystemPrompt.Text);
        else
            sb.AppendLineLinux($"This is a conversation between {chat.User.Name} and {character.Name}. You are playing the role of {character.Name}. The current date and time is {DateTime.Now.ToString("f", chat.CultureInfo)}.  Keep the conversation flowing, actively engage with {chat.User.Name}. Stay in character.");
        
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
        sb.AppendLineLinux($"Only write a single reply from {character.Name} for natural speech.");
        return sb.ToString().TrimExcess();
    }

    private static string MakePostHistoryPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        if (!character.PostHistoryInstructions.HasValue) return "";
        var sb = new StringBuilder();
        if (!character.PostHistoryInstructions.HasValue)
            sb.AppendLineLinux(character.PostHistoryInstructions.Text);
        return sb.ToString().TrimExcess();
    }
}