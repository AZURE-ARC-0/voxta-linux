using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LLMUtils;

public static class TokenizerFactory
{
    public static ITokenizer GetDefault() => new AverageTokenizer();
}