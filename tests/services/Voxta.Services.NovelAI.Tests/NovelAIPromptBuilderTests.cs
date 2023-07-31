using Voxta.Abstractions.Model;
using Voxta.Services.OpenSourceLargeLanguageModels;

namespace Voxta.Services.OpenAI.Tests;

public class NovelAIPromptBuilderTests
{
    private NovelAIPromptBuilder _builder = null!;

    [SetUp]
    public void Setup()
    {
        _builder = new NovelAIPromptBuilder();
    }

    [Test]
    public void TestPromptMinimal()
    {
        var actual = _builder.BuildReplyPrompt(new ChatSessionData
        {
            UserName = "Joe",
            Character = new()
            {
                Name = "Jane",
                Description = "some-description",
                Personality = "some-personality",
                Scenario = "some-scenario",
                FirstMessage = "some-first-message",
            },
            Messages =
            {
                new ChatMessageData { User = "Joe", Text = "Hello" },
                new ChatMessageData { User = "Jane", Text = "World" },
                new ChatMessageData { User = "Joe", Text = "Question" },
            }
        }, 4096);

        Assert.That(actual, Is.EqualTo("""
        Description of Jane: some-description
        Personality of Jane: some-personality
        Circumstances and context of the dialogue: some-scenario
        ***
        [ Style: chat ]
        Joe: Hello
        Jane: World
        Joe: Question
        Jane: 
        """.ReplaceLineEndings("\n").TrimExcess()));
    }
}