using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.OpenAI;

public class OpenAIPromptBuilder : MessagePromptBuilder
{
    public OpenAIPromptBuilder(ITokenizer tokenizer)
        : base(tokenizer)
    {
    }

    public OpenAIPromptBuilder(ITokenizer tokenizer, ITimeProvider timeProvider)
    : base(tokenizer, timeProvider)
    {
    }

    protected override int GetMessageTokens(string user, string message, int messageTokens)
    {
        // https://github.com/openai/openai-python/blob/main/chatml.md
        return messageTokens + 4;
    }
}