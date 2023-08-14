using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LLMUtils;

public class AverageTokenizer : ITokenizer
{
    private const double AverageTokenPerCharacter = 0.25d;

    public int CountTokens(string value)
    {
        var count = (int)Math.Ceiling(value.Length * AverageTokenPerCharacter);
        return count;
    }

    public IList<int> Tokenize(string value)
    {
        throw new NotSupportedException("Cannot tokenize using the average tokenizer");
    }
}
