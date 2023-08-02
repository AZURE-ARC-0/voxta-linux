using System.Diagnostics.CodeAnalysis;

namespace Voxta.Abstractions.Tokenizers;

public interface ITokenizer
{
    int CountTokens(string value);
}
