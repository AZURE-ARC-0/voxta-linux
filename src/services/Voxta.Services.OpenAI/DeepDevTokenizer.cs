using Microsoft.DeepDev;

namespace Voxta.Services.OpenAI;

public class DeepDevTokenizer : Voxta.Abstractions.Tokenizers.ITokenizer
{
    public static Voxta.Abstractions.Tokenizers.ITokenizer Create()
    {
        var deepDevTokenizer = TokenizerBuilder.CreateByModelName("gpt-3.5-turbo", OpenAISpecialTokens.SpecialTokens);
        return new DeepDevTokenizer(deepDevTokenizer);
    }
    
    private readonly ITokenizer _deepDevTokenizer;

    public DeepDevTokenizer(ITokenizer deepDevTokenizer)
    {
        _deepDevTokenizer = deepDevTokenizer;
    }
    
    public int CountTokens(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return _deepDevTokenizer.Encode(value, OpenAISpecialTokens.Keys).Count;
    }

    public IList<int> Tokenize(string value)
    {
        return _deepDevTokenizer.Encode(value, OpenAISpecialTokens.Keys);
    }
}