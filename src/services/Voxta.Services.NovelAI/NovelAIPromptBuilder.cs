using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.OpenSourceLargeLanguageModels;

public class NovelAIPromptBuilder : GenericPromptBuilder
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