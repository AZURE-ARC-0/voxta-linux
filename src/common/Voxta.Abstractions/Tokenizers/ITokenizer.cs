namespace Voxta.Abstractions.Tokenizers;

public interface ITokenizer
{
    int CountTokens(string value);
    IList<int> Tokenize(string value);
}
