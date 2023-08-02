using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LargeLanguageModelsUtils;

public static class TokenizerFactory
{
    public static ITokenizer GetDefault() => new AverageTokenizer();
}