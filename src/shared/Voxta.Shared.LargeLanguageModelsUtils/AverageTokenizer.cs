using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LargeLanguageModelsUtils;

public class AverageTokenizer : ITokenizer
{
    private const double _averageTokenPerCharacter = 0.25d;

    public int CountTokens(string value)
    {
        var count = (int)Math.Ceiling(value.Length * _averageTokenPerCharacter);
        return count;
    }
}
