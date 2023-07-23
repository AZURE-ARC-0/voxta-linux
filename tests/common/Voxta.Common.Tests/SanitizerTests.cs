namespace ChatMate.Common.Tests;

public class SanitizerTests
{
    private Sanitizer _sanitizer = null!;

    [SetUp]
    public void Setup()
    {
        _sanitizer = new Sanitizer();
    }
    
    [TestCase("hello\n", "hello.")]
    [TestCase("\"hello.\"", "hello.")]
    [TestCase("Hello. World", "Hello.")]
    [TestCase("Hello. World.\"", "Hello. World.")]
    public void TestSanitize(string input, string expected)
    {
        Assert.That(_sanitizer.Sanitize(input), Is.EqualTo(expected));
    }
}