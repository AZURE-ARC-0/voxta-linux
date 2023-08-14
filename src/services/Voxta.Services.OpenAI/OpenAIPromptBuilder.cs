using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;
using Voxta.Services.MessageBased;

namespace Voxta.Services.OpenAI;

public class OpenAIPromptBuilder : MessageBasedPromptBuilder
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