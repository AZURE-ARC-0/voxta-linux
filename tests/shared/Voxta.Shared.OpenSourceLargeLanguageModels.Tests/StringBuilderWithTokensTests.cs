using Moq;
using NUnit.Framework;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Tokenizers;
using Voxta.Shared.LLMUtils;

namespace Voxta.Shared.OpenSourceLargeLanguageModels.Tests;

public class StringBuilderWithTokensTests
{
    private Mock<ITokenizer> _tokenizer = null!;

    [SetUp]
    public void Setup()
    {
        _tokenizer = new Mock<ITokenizer>();
        _tokenizer.Setup(m => m.CountTokens(It.IsAny<string>())).Returns((string value) => value.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length);
    }
    
    [Test]
    public void Empty()
    {
        var sb = new StringBuilderWithTokens(_tokenizer.Object);
        
        var textData = sb.ToTextData();
        
        Assert.Multiple(() =>
        {
            Assert.That(sb.Tokens, Is.EqualTo(0));
            Assert.That(textData.Tokens, Is.EqualTo(0));
            Assert.That(textData.Value, Is.EqualTo(""));
        });
    }
    
    [Test]
    public void AppendString()
    {
        var sb = new StringBuilderWithTokens(_tokenizer.Object);

        sb.Append("One ");
        sb.AppendLineLinux("Two");
        sb.AppendLineLinux("Three");
        var textData = sb.ToTextData();
        
        Assert.Multiple(() =>
        {
            Assert.That(sb.Tokens, Is.EqualTo(5));
            Assert.That(textData.Tokens, Is.EqualTo(4));
            Assert.That(textData.Value, Is.EqualTo("One Two\nThree"));
        });
    }
    
    [Test]
    public void AppendData()
    {
        var sb = new StringBuilderWithTokens(_tokenizer.Object);

        sb.Append(new TextData { Value = "One ", Tokens = 1 });
        sb.AppendLineLinux(new TextData { Value = "Two", Tokens = 2 });
        sb.AppendLineLinux(new TextData { Value = "Three", Tokens = 3 });
        var textData = sb.ToTextData();
        
        Assert.Multiple(() =>
        {
            Assert.That(sb.Tokens, Is.EqualTo(8));
            Assert.That(textData.Tokens, Is.EqualTo(7));
            Assert.That(textData.Value, Is.EqualTo("One Two\nThree"));
        });
    }
}