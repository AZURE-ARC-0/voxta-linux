using System.Text;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LargeLanguageModelsUtils;

public class GenericPromptBuilder
{
    private readonly ITokenizer _tokenizer;
    private readonly ITimeProvider _timeProvider;

    public GenericPromptBuilder(ITokenizer tokenizer)
        : this(tokenizer, TimeProvider.Current)
    {
    }

    public GenericPromptBuilder(ITokenizer tokenizer, ITimeProvider timeProvider)
    {
        _tokenizer = tokenizer;
        _timeProvider = timeProvider;
    }
    
    public string BuildReplyPrompt(IChatInferenceData chat, int maxMemoryTokens, int maxTokens, bool includePostHistoryPrompt = true)
    {
        if (maxTokens <= 0) throw new ArgumentException("Max tokens must be larger than zero.", nameof(maxTokens));
        if (maxMemoryTokens >= maxTokens) throw new ArgumentException("Cannot have more memory tokens than the max tokens.", nameof(maxMemoryTokens));
        
        var sb = new StringBuilderWithTokens(_tokenizer, maxTokens);
        var systemPrompt = MakeSystemPrompt(chat);
        sb.AppendLineLinux(systemPrompt);
        var query = $"{chat.Character.Name}:";
        var queryTokens = _tokenizer.CountTokens(query);
        var userSuffix = new TextData
        {
            Value = ": ",
            Tokens = _tokenizer.CountTokens(": "),
        };
        sb.Reserve(queryTokens);
        var tokensPerUser = new Dictionary<string, TextData>();

        var memorySb = new StringBuilderWithTokens(_tokenizer, maxMemoryTokens);
        if (maxMemoryTokens > 0)
        {
            var memories = chat.GetMemories();
            if (memories.Count > 0)
            {
                memorySb.AppendLineLinux($"What {chat.Character.Name} knows:");
                foreach (var memory in memories)
                {
                    if (!memorySb.AppendLineLinux(memory.Text)) break;
                }
            }
            memorySb.AppendLineLinux();
        }

        if (memorySb.Tokens > 0)
        {
            sb.AppendLineLinux(memorySb.ToTextData());
        }

        var chatMessages = chat.GetMessages();
        var includedMessages = new List<TextData>();
        var tokensBudget = maxTokens - sb.Tokens;
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            if (!tokensPerUser.TryGetValue(message.User, out var userData))
            {
                userData = new TextData
                {
                    Value = message.User,
                    Tokens = _tokenizer.CountTokens(message.User),
                };
                tokensPerUser.Add(message.User, userData);
            }
            var messageData = new TextData
            {
                Value = message.User + userSuffix + message.Value + '\n',
                Tokens = userData.Tokens + userSuffix.Tokens + _tokenizer.CountTokens(message.Value) + 1,
            };
            tokensBudget -= messageData.Tokens;
            if(tokensBudget < 0) break;
            includedMessages.Insert(0, messageData);
        }
        
        if (includedMessages.Count < Math.Min(chatMessages.Count, 4))
            throw new InvalidOperationException($"Reached {maxTokens} before writing at least two message rounds, which will result in incoherent conversations. Either increase max tokens ({sb.Tokens} / {maxTokens}) and/or reduce memory tokens ({memorySb.Tokens} / {maxMemoryTokens}).");

        foreach(var messageData in includedMessages)
        {
            sb.Append(messageData);
        }

        sb.Release(queryTokens);
        sb.Append(query);

        return sb.ToTextData().Value;
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
        sb.AppendLineLinux();
        sb.Append("Action: [");
        return sb.ToString().TrimExcess();
    }

    public string[] SummarizationStopTokens => new[] { "\n\n" }; 

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
            sb.AppendLineLinux($"{message.User}: {message.Value}");
        }
        
        sb.AppendLineLinux("<END>");
        sb.AppendLineLinux();
        sb.AppendLineLinux("Facts learned:");
        return sb.ToString();
    }

    protected virtual TextData MakeSystemPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        var sb = new StringBuilderWithTokens(_tokenizer);
        if (character.SystemPrompt.HasValue)
            sb.AppendLineLinux(character.SystemPrompt.Value);
        else
            sb.AppendLineLinux($"This is a spoken conversation between {chat.User.Name} and {character.Name}. You are playing the role of {character.Name}. The current date and time is {_timeProvider.LocalNow.ToString("f", chat.CultureInfo)}. Keep the conversation flowing, actively engage with {chat.User.Name}. Stay in character. Emojis are prohibited, only use spoken words. Avoid making up facts about {chat.User.Name}.");
        
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
        return sb.ToTextData();
    }

    private TextData MakePostHistoryPrompt(IChatInferenceData chat)
    {
        var character = chat.Character;
        var sb = new StringBuilderWithTokens(_tokenizer);
        if (character.PostHistoryInstructions.HasValue)
            sb.AppendLineLinux(string.Join("\n", character.PostHistoryInstructions.Value.Split(new[]{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries).Select(x => $"({x})")));
        return sb.ToTextData();
    }
}
