using System.Text;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.NovelAI;

public class NovelAIPromptBuilder : TextPromptBuilder
{
    private readonly ITokenizer _tokenizer;

    public NovelAIPromptBuilder(ITokenizer tokenizer)
        : base(tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public NovelAIPromptBuilder(ITokenizer tokenizer, ITimeProvider timeProvider)
        : base(tokenizer, timeProvider)
    {
        _tokenizer = tokenizer;
    }
    
    protected override void FormatMessage(StringBuilder sb, IChatInferenceData chat, MessageData message)
    {
        if (message.Role == ChatMessageRole.System)
        {
            sb.AppendLineLinux(message.Value);
            return;
        }
        
        sb.Append(message.Role switch
        {
            ChatMessageRole.Assistant => chat.Character.Name,
            ChatMessageRole.User => chat.User.Name,
            _ => throw new ArgumentOutOfRangeException(null, $"Unknown role: {message.Role}")
        });
        sb.Append(": ");
        sb.AppendLineLinux(message.Value);
    }

    // https://docs.novelai.net/text/chatformat.html
    protected override TextData MakeSystemPrompt(IChatInferenceData chat)
    {
        var prompt = base.MakeSystemPrompt(chat);
        const string suffix = "\n***\n[ Style: chat ]";
        var suffixTokens = _tokenizer.CountTokens(suffix);
        return new TextData
        {
            Value = prompt.Value + suffix,
            Tokens = prompt.Tokens + suffixTokens,
        };
    }
}