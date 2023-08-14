using NUnit.Framework;

namespace Voxta.Shared.LLMUtils.Tests;

public class AverageTokenizerTests
{
    private AverageTokenizer _tokenizer = null!;

    [SetUp]
    public void SetUp()
    {
        _tokenizer = new AverageTokenizer();
    }

    [Test]
    public void AverageTokensValue()
    {
        var tokens = _tokenizer.CountTokens("I am writing this text.");
        
        Assert.That(tokens, Is.EqualTo(6));
    }
}