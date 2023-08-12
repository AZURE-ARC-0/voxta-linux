using NUnit.Framework;
using Voxta.Abstractions.Model;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Shared.OpenSourceLargeLanguageModels.Tests;

public class GenericPromptBuilderTests
{
    private GenericPromptBuilder _builder = null!;

    [SetUp]
    public void Setup()
    {
        var tokenizer = new AverageTokenizer();
        _builder = new GenericPromptBuilder(tokenizer);
    }

    [Test]
    public void TestPromptMinimal()
    {
        var chat = new ChatSessionData
            {
                UserName = "Joe",
                Chat = new Chat
                {
                    Id = Guid.Empty,
                    CharacterId = Guid.Empty,
                },
                Character = new()
                {
                    Name = "Jane",
                    Description = "some-description",
                    Personality = "some-personality",
                    Scenario = "some-scenario",
                    FirstMessage = "some-first-message",
                    Services = null!,
                }
            }
            .AddMessage("Joe", "Hello")
            .AddMessage("Jane", "World")
            .AddMessage("Joe", "Question");
        
        var actual = _builder.BuildReplyPrompt(chat, 0, 4096);

        Assert.That(actual, Is.EqualTo("""
        Description of Jane: some-description
        Personality of Jane: some-personality
        Circumstances and context of the dialogue: some-scenario
        Joe: Hello
        Jane: World
        Joe: Question
        Jane: 
        """.ReplaceLineEndings("\n").TrimExcess()));
    }
    
    [Test]
    public void TestPromptFull()
    {
        var chat = new ChatSessionData
            {
                UserName = "Joe",
                Chat = new Chat
                {
                    Id = Guid.Empty,
                    CharacterId = Guid.Empty,
                },
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
                    Services = null!,
                },
                Actions = new[] { "action1", "action2" },
                Context = "some-context",
            }
            .AddMessage("Joe", "Hello")
            .AddMessage("Jane", "World")
            .AddMessage("Joe", "Question");
        
        var actual = _builder.BuildReplyPrompt(chat, 0, 4096);

        Assert.That(actual, Is.EqualTo("""
        some-system-prompt
        
        Description of Jane: some-description
        Personality of Jane: some-personality
        Circumstances and context of the dialogue: some-scenario
        some-context
        Optional actions Jane can do: [action1], [action2]
        Joe: Hello
        Jane: World
        Joe: Question
        Jane: 
        """.ReplaceLineEndings("\n").TrimExcess()));
    }

    [Test]
    public void TestPromptMemory()
    {
        var chat = new ChatSessionData
            {
                UserName = "Joe",
                Chat = new Chat
                {
                    Id = Guid.Empty,
                    CharacterId = Guid.Empty,
                },
                Character = new()
                {
                    Name = "Jane",
                    Description = "some-description",
                    Personality = "some-personality",
                    Scenario = "some-scenario",
                    FirstMessage = "some-first-message",
                    Services = null!,
                }
            }
            .AddMessage("Joe", "Hello")
            .AddMessage("Jane", "World")
            .AddMessage("Joe", "Question");
        chat.Memories.Add(new MemoryItem { Id = Guid.Empty, Keywords = Array.Empty<string>(), Text = "memory-1", Weight = 0 });
        chat.Memories.Add(new MemoryItem { Id = Guid.Empty, Keywords = Array.Empty<string>(), Text = "memory-2", Weight = 0 });
        chat.Memories.Add(new MemoryItem { Id = Guid.Empty, Keywords = Array.Empty<string>(), Text = "memory-3", Weight = 0 });
        
        var actual = _builder.BuildReplyPrompt(chat, 1024, 4096);

        Assert.That(actual, Is.EqualTo("""
           Description of Jane: some-description
           Personality of Jane: some-personality
           Circumstances and context of the dialogue: some-scenario
           What Jane knows:
           memory-1
           memory-2
           memory-3
           Joe: Hello
           Jane: World
           Joe: Question
           Jane:
           """.ReplaceLineEndings("\n").TrimExcess()));
    }
    
    [Test]
    public void TestPromptActionInference()
    {
        var chat = new ChatSessionData
            {
                UserName = "Joe",
                Chat = new Chat
                {
                    Id = Guid.Empty,
                    CharacterId = Guid.Empty,
                },
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
                    Services = null!,
                },
                Actions = new[] { "action1", "action2" },
                Context = "some-context",
            }
            .AddMessage("Joe", "Hello")
            .AddMessage("Jane", "World")
            .AddMessage("Joe", "Question");
        var actual = _builder.BuildActionInferencePrompt(chat);

        Assert.That(actual, Is.EqualTo("""
        You are tasked with inferring the best action from a list based on the content of a sample chat.

        Actions: [action1], [action2]
        Conversation Context:
        Jane's Personality: some-personality
        Scenario: some-scenario
        Context: some-context

        Conversation:
        Joe: Hello
        Jane: World
        Joe: Question
        
        Based on the last message, which of the following actions is the most applicable for Jane: [action1], [action2]

        Only write the action.

        Action: [
        """.ReplaceLineEndings("\n").TrimExcess()));
    }
    
    [Test]
    public void TestPromptSummarization()
    {
        var chat = new ChatSessionData
            {
                UserName = "Joe",
                Chat = new Chat
                {
                    Id = Guid.Empty,
                    CharacterId = Guid.Empty,
                },
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
                    Services = null!,
                },
                Actions = new[] { "action1", "action2" },
                Context = "some-context",
            }
            .AddMessage("Joe", "Hello")
            .AddMessage("Jane", "World")
            .AddMessage("Joe", "Question");
        var actual = _builder.BuildSummarizationPrompt(chat);

        Assert.That(actual, Is.EqualTo("""
            Memorize new knowledge Jane learned in the conversation.

            Use as few words as possible.
            Write from the point of view of Jane.
            Use telegraphic dense notes style.
            Associate memory with the right person.
            Only write useful and high confidence memories.
            These categories are the most useful: physical descriptions, emotional state, relationship progression, gender, sexual orientation, preferences, events, state of the participants.

            <START>

            Joe: Hello
            Jane: World
            Joe: Question

            What Jane learned:

           """.ReplaceLineEndings("\n").TrimExcess()));
    }
}
