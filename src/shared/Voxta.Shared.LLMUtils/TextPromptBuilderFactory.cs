using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LLMUtils;

public static class TextPromptBuilderFactory
{
    public static TextPromptBuilder Create(PromptFormats format, ITokenizer tokenizer)
    {
        return format switch
        {
            PromptFormats.Generic => new TextPromptBuilder(tokenizer),
            PromptFormats.Alpaca => new AlpacaPromptBuilder(tokenizer),
            PromptFormats.Llama2 => new Llama2PromptBuilder(tokenizer),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
}