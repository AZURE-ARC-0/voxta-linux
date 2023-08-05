using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LargeLanguageModelsUtils;

public class AverageTokenizer : ITokenizer
{
    private const double AverageTokenPerCharacter = 0.25d;

    public int CountTokens(string value)
    {
        var count = (int)Math.Ceiling(value.Length * AverageTokenPerCharacter);
        return count;
    }
}
