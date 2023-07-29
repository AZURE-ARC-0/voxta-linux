using System.Diagnostics.CodeAnalysis;

namespace Voxta.Services.TextToSpeechUtils.Tests;

public class TextToSpeechPreprocessorTests
{
    private TextToSpeechPreprocessor _preprocessor = null!;

    [SetUp]
    public void Setup()
    {
        _preprocessor = new TextToSpeechPreprocessor();
    }
    
    [TestCase("en-US", "Hello!", "Hello!")]
    [TestCase("en-US", "I like NovelAI. It's a good AI service.", "I like Novel AI. It's a good AI service.")]
    [TestCase("en-US", "I took 1002 items!", "I took one thousand and two items!")]
    [TestCase("es-SP", "I have 1000000 items.", "I have un millón items.")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public void TestPreprocessing(string culture, string input, string expected)
    {
        var actual = _preprocessor.Preprocess(input, culture);
        
        Assert.That(actual, Is.EqualTo(expected));
    }
}