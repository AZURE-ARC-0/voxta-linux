﻿using System.Text;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LLMUtils;

public class TextPromptBuilder : MessagePromptBuilder
{
    public string[] SummarizationStopTokens => new[] { "\n\n" };
    
    protected override bool PostHistorySupport => false;
    
    private readonly ITokenizer _tokenizer;

    private readonly Dictionary<string, int> _userPrefixTokens = new();

    public TextPromptBuilder(ITokenizer tokenizer)
        : base(tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public TextPromptBuilder(ITokenizer tokenizer, ITimeProvider timeProvider)
        : base(tokenizer, timeProvider)
    {
        _tokenizer = tokenizer;
    }

    protected override int GetMessageTokens(string user, string message, int messageTokens)
    {
        if (_userPrefixTokens.TryGetValue(user, out var tokens))
            return tokens + messageTokens;
        tokens = _tokenizer.CountTokens(user + ": ");
        _userPrefixTokens[user] = tokens;
        return tokens + messageTokens;
    }

    public string BuildReplyPromptString(IChatInferenceData chat, int maxMemoryTokens, int maxTokens)
    {
        var query = $"{chat.Character.Name}:";
        var queryTokens = _tokenizer.CountTokens(query);
        var messages = BuildReplyPrompt(chat, maxMemoryTokens, maxTokens - queryTokens);
        return StringifyMessages(chat, messages, query);
    }

    public string BuildActionInferencePromptString(IChatInferenceData chat)
    {
        var messages = BuildActionInferencePrompt(chat);
        return StringifyMessages(chat, messages, "\nAction: [");
    }

    public string BuildSummarizationPromptString(IChatInferenceData chat, IReadOnlyList<ChatMessageData> messagesToSummarize)
    {
        var messages = BuildSummarizationPrompt(chat, messagesToSummarize);
        return StringifyMessages(chat, messages, "");
    }

    private string StringifyMessages(IChatInferenceData chat, List<MessageData> messages, string query)
    {
        var sb = new StringBuilder(messages.Sum(m => m.Value.Length + 4));
        BeginMessages(sb);
        foreach (var message in messages)
        {
            FormatMessage(sb, chat, message);
        }
        EndMessages(sb, query);
        return sb.ToString();
    }

    protected virtual void BeginMessages(StringBuilder sb)
    {
    }

    protected virtual void EndMessages(StringBuilder sb, string query)
    {
        sb.Append(query);
    }

    protected virtual void FormatMessage(StringBuilder sb, IChatInferenceData chat, MessageData message)
    {
        sb.Append(message.Role switch
        {
            ChatMessageRole.System => "System",
            ChatMessageRole.Assistant => chat.Character.Name,
            ChatMessageRole.User => chat.User.Name,
            _ => throw new ArgumentOutOfRangeException(null, $"Unknown role: {message.Role}")
        });
        sb.Append(": ");
        sb.AppendLineLinux(message.Value);
    }

    public virtual string[] GetReplyStoppingStrings(IChatInferenceData chat)
    {
        return new[] { "END_OF_DIALOG", $"{chat.User.Name}:", $"{chat.Character.Name}:", "\n" };
    }
}
