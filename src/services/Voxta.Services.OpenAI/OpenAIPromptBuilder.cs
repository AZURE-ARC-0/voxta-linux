using System.Text;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.OpenAI;

public class OpenAIPromptBuilder
{
    private readonly ITimeProvider _timeProvider;
    private readonly ITokenizer _tokenizer;

    public OpenAIPromptBuilder(ITokenizer tokenizer)
        : this(tokenizer, TimeProvider.Current)
    {
    }

    public OpenAIPromptBuilder(ITokenizer tokenizer, ITimeProvider timeProvider)
    {
        _tokenizer = tokenizer;
        _timeProvider = timeProvider;
    }

    public List<OpenAIMessage> BuildReplyPrompt(IChatInferenceData chat, int maxMemoryTokens, int maxTokens)
    {
        var systemPrompt = MakeSystemPrompt(chat);
        var postHistoryPrompt = MakePostHistoryPrompt(chat);

        var totalTokens = systemPrompt.Tokens + postHistoryPrompt.Tokens;

        var memorySb = new StringBuilderWithTokens(_tokenizer, maxMemoryTokens);
        if (maxMemoryTokens > 0)
        {
            var memories = chat.GetMemories();
            if (memories.Count > 0)
            {
                memorySb.AppendLineLinux();
                memorySb.AppendLineLinux($"What {chat.Character.Name} knows:");
                foreach (var memory in memories)
                {
                    if (!memorySb.AppendLineLinux(memory.Text)) break;
                }
            }
        }

        if (memorySb.Tokens > 0)
        {
            var memoryData = memorySb.ToTextData();
            systemPrompt += memoryData.Value;
            totalTokens += memoryData.Tokens;
        }
        
        var messages = new List<OpenAIMessage> { new() { role = "system", content = systemPrompt } };
        var chatMessages = chat.GetMessages();
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            totalTokens += message.Tokens + 4; // https://github.com/openai/openai-python/blob/main/chatml.md
            if (totalTokens >= maxTokens) break;
            var role = message.User == chat.Character.Name.Value ? "assistant" : "user";
            messages.Insert(1, new() { role = role, content = message.Value });
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
            sb.AppendLineLinux($"{message.User}: {message.Value}");
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

    public List<OpenAIMessage> BuildSummarizationPrompt(IChatInferenceData chat, List<ChatMessageData> messagesToSummarize)
    {
        var messages = new List<OpenAIMessage>
        {
            new() {
                role = "system",
                content = """
                  You are tasked with extracting knowledge from a conversation for memorization.
                  """.ReplaceLineEndings("\n")
            },
        };
        
        var sb = new StringBuilder();
        sb.AppendLineLinux($"""
            You must write facts about {chat.Character.Name} and {chat.User.Name} from their conversation.
            Facts must be short. Be specific. Write in a way that identifies the user associated with the fact. Use words from the conversation when possible.
            Prefer facts about: physical descriptions, emotional state, relationship progression, gender, sexual orientation, preferences, events.
            Write the most useful facts first.

            <START>
            """.ReplaceLineEndings("\n"));
        
        foreach (var message in messagesToSummarize)
        {
            sb.AppendLineLinux($"{message.User}: {message.Value}");
        }
        
        sb.AppendLineLinux("<END>");
        sb.AppendLineLinux();
        sb.AppendLineLinux("Facts learned:");
        
        messages.Add(new OpenAIMessage { role = "user", content = sb.ToString().TrimExcess() });

        return messages;
    }

    private TextData MakeSystemPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        var sb = new StringBuilderWithTokens(_tokenizer);
        if (character.SystemPrompt.HasValue)
            sb.AppendLineLinux(character.SystemPrompt.Value);
        else
            sb.AppendLineLinux($"This is a conversation between {chat.User.Name} and {character.Name}. You are playing the role of {character.Name}. The current date and time is {_timeProvider.LocalNow.ToString("f", chat.CultureInfo)}.  Keep the conversation flowing, actively engage with {chat.User.Name}. Stay in character.");
        
        sb.AppendLineLinux();
        
        if (chat.User.Description.HasValue)
            sb.AppendLineLinux($"Description of {chat.User.Name}: {chat.User.Description}");
        if (character.Description.HasValue)
            sb.AppendLineLinux($"Description of {character.Name}: {character.Description}");
        if (character.Personality.HasValue)
            sb.AppendLineLinux($"Personality of {character.Name}: {character.Personality}");
        if (character.Scenario.HasValue)
            sb.AppendLineLinux($"Circumstances and context of the dialogue: {character.Scenario}");
        if (chat.Context?.HasValue == true)
            sb.AppendLineLinux(chat.Context.Value);
        if (chat.Actions is { Length: > 1 })
            sb.AppendLineLinux($"Optional actions {character.Name} can do: {string.Join(", ", chat.Actions.Select(x => $"[{x}]"))}");
        sb.AppendLineLinux($"Only write a single reply from {character.Name} for natural speech.");
        return sb.ToTextData();
    }

    private TextData MakePostHistoryPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        if (!character.PostHistoryInstructions.HasValue) return TextData.Empty;
        var sb = new StringBuilderWithTokens(_tokenizer);
        if (!character.PostHistoryInstructions.HasValue)
            sb.AppendLineLinux(character.PostHistoryInstructions.Value);
        return sb.ToTextData();
    }
}