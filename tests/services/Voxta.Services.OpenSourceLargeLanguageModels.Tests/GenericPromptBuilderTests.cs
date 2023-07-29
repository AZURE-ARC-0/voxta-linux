using Voxta.Abstractions.Model;
using NUnit.Framework;

namespace Voxta.Services.OpenAI.Tests;

public class GenericPromptBuilderTests
{
    private GenericPromptBuilder _builder = null!;

    [SetUp]
    public void Setup()
    {
        _builder = new GenericPromptBuilder();
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
        Joe: "Hello"
        Jane: "World"
        Joe: "Question"
        Jane: "
        """.ReplaceLineEndings("\n").TrimExcess()));
    }
    
    [Test]
    public void TestPromptFull()
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
                SystemPrompt = "some-system-prompt",
                PostHistoryInstructions = "some-post-history-instructions",
                MessageExamples = "Joe: Request\nJane: Response",
            },
            Messages =
            {
                new ChatMessageData { User = "Joe", Text = "Hello" },
                new ChatMessageData { User = "Jane", Text = "World" },
                new ChatMessageData { User = "Joe", Text = "Question" },
            },
            Actions = new[] { "action1", "action2" },
            Context = "some-context",
        }, 4096);

        Assert.That(actual, Is.EqualTo("""
        some-system-prompt
        Description of Jane: some-description
        Personality of Jane: some-personality
        Circumstances and context of the dialogue: some-scenario
        Joe: "Hello"
        Jane: "World"
        Joe: "Question"
        some-post-history-instructions
        Current context: some-context
        Available actions to be inferred after the response: action1, action2
        Jane: "
        """.ReplaceLineEndings("\n").TrimExcess()));
    }
    
    [Test]
    public void TestPromptActionInference()
    {
        var actual = _builder.BuildActionInferencePrompt(new ChatSessionData
        {
            UserName = "Joe",
            Character = new()
            {
                Name = "Jane",
                Description = "some-description",
                Personality = "some-personality",
                Scenario = "some-scenario",
                FirstMessage = "some-first-message",
                SystemPrompt = "some-system-prompt",
                PostHistoryInstructions = "some-post-history-instructions",
                MessageExamples = "Joe: Request\nJane: Response",
            },
            Messages =
            {
                new ChatMessageData { User = "Joe", Text = "Hello" },
                new ChatMessageData { User = "Jane", Text = "World" },
                new ChatMessageData { User = "Joe", Text = "Question" },
            },
            Actions = new[] { "action1", "action2" },
            Context = "some-context",
        });

        Assert.That(actual, Is.EqualTo("""
        You are a tool that selects the character animation for a Virtual Reality game. You will be presented with a chat, and must provide the animation to play from the provided list. Only answer with a single animation name. Example response: [smile]
        Jane's Personality: some-personality
        Scenario: some-scenario
        Previous messages:
        Joe: Hello
        Jane: World
        Joe: Question
        ---
        Context: some-context
        Available actions: [action1], [action2]
        Write the action Jane should play.
        Action: [
        """.ReplaceLineEndings("\n").TrimExcess()));
    }
}