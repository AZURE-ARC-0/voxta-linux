using Microsoft.DeepDev;

namespace Voxta.Services.OpenAI;

public class DeepDevTokenizer : Voxta.Abstractions.Tokenizers.ITokenizer
{
    private static readonly Lazy<Task<Voxta.Abstractions.Tokenizers.ITokenizer>> Shared = new(CreateAsync);
    
    public static Task<Voxta.Abstractions.Tokenizers.ITokenizer> GetSharedInstanceAsync() => Shared.Value;

    private static async Task<Voxta.Abstractions.Tokenizers.ITokenizer> CreateAsync()
    {
        var deepDevTokenizer = await TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo", OpenAISpecialTokens.SpecialTokens);
        return new DeepDevTokenizer(deepDevTokenizer);
    }
    
    private readonly ITokenizer _deepDevTokenizer;

    private DeepDevTokenizer(ITokenizer deepDevTokenizer)
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