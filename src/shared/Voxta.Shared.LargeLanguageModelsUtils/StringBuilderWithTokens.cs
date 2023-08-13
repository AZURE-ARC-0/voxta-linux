using System.Text;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LargeLanguageModelsUtils;

public class StringBuilderWithTokens
{
    private readonly ITokenizer _tokenizer;
    private readonly int _maxTokens;
    private readonly StringBuilder _sb = new();
    
    public int Tokens { get; private set; }

    public StringBuilderWithTokens(ITokenizer tokenizer, int maxTokens = int.MaxValue)
    {
        _tokenizer = tokenizer;
        _maxTokens = maxTokens;
    }

    public void Reserve(int tokens)
    {
        Tokens += tokens;
    }
    
    public void Release(int tokens)
    {
        Tokens -= tokens;
    }
    
    public bool Append(string value)
    {
        var total = Tokens + _tokenizer.CountTokens(value);
        if (total > _maxTokens) return false;
        _sb.Append(value);
        Tokens = total;
        return true;
    }
    
    public bool Append(TextData data)
    {
        var total = Tokens + data.Tokens;
        if (total > _maxTokens) return false;
        _sb.Append(data.Value);
        Tokens = total;
        return true;
    }
    
    public bool AppendLineLinux()
    {
        if (Tokens + 1 > _maxTokens)
            return false;
        _sb.AppendLineLinux();
        Tokens++;
        return true;
    }
    
    public bool AppendLineLinux(string value)
    {
        var total = Tokens + _tokenizer.CountTokens(value) + 1;
        if (total > _maxTokens) return false;
        _sb.AppendLineLinux(value);
        Tokens = total;
        return true;
    }
    
    public bool AppendLineLinux(TextData data)
    {
        var total = Tokens + data.Tokens + 1;
        if (total > _maxTokens) return false;
        _sb.AppendLineLinux(data.Value);
        Tokens = total;
        return true;
    }
    
    public TextData ToTextData()
    {
        var value = _sb.ToString();
        var tokens = Tokens;
        if (value.EndsWith('\n'))
        {
            value = value[..^1];
            tokens--;
        }
        return new TextData { Value = value, Tokens = tokens };
    }
}