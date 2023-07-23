namespace ChatMate.Common.Tests;

public class StringExtensionsTests
{
    [Test]
    public void TestTrimExcess()
    {
        Assert.That("\r\nhello \n\r".TrimExcess(), Is.EqualTo("hello"));
    }
    
    [TestCase("smile", "smile", 0)]
    [TestCase("smile", "smiling", 3)]
    [TestCase("smile", "frown", 5)]
    public void TestLevenshteinDistance(string source, string value, int expected)
    {
        Assert.That(source.GetLevenshteinDistance(value), Is.EqualTo(expected));
    }
}