using Moq;
using NUnit.Framework;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;

namespace Voxta.Shared.LLMUtils.Tests;

public class GenericPromptBuilderTests
{
    private GenericPromptBuilder _builder = null!;

    [SetUp]
    public void Setup()
    {
        var tokenizer = new AverageTokenizer();
        var timeProvider = new Mock<ITimeProvider>();
        timeProvider.Setup(m => m.LocalNow).Returns(new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero));
        _builder = new GenericPromptBuilder(tokenizer, timeProvider.Object);
    }

    [Test]
    public void TestPromptMinimal()
    {
        var character = new ChatSessionDataCharacter
        {
            Name = "Jane",
            Description = "some-description",
            Personality = "some-personality",
            Scenario = "some-scenario",
            FirstMessage = "some-first-message",
        };
        var user = new ChatSessionDataUser { Name = "Joe" };
        var chat = new ChatSessionData
        {
            Culture = "en-US",
            User = user,
            Chat = new Chat
            {
                Id = Guid.Empty,
                CharacterId = Guid.Empty,
            },
            Character = character,
        };
        chat.AddMessage(user, "Hello");
        chat.AddMessage(character, "World");
        chat.AddMessage(user, "Question");
        
        var actual = _builder.BuildReplyPromptString(chat, 0, 4096);

        Assert.That(actual, Is.EqualTo("""
        This is a spoken conversation between Joe and Jane. You are playing the role of Jane. The current date and time is Saturday, February 3, 2001 4:05 AM. Keep the conversation flowing, actively engage with Joe. Stay in character. Emojis are prohibited, only use spoken words. Avoid making up facts about Joe.
        
        Description of Jane: some-description
        Personality of Jane: some-personality
        Circumstances and context of the dialogue: some-scenario
        Joe: Hello
        Jane: World
        Joe: Question
        Jane:
        """.ReplaceLineEndings("\n")));
    }
    
    [Test]
    public void TestPromptFull()
    {
        var character = new ChatSessionDataCharacter
        {
            Name = "Jane",
            Description = "some-description",
            Personality = "some-personality",
            Scenario = "some-scenario",
            FirstMessage = "some-first-message",
            SystemPrompt = "some-system-prompt",
            PostHistoryInstructions = "some-post-history-instructions",
            MessageExamples = "Joe: Request\nJane: Response",
        };
        var user = new ChatSessionDataUser { Name = "Joe" };
        var chat = new ChatSessionData
        {
            Culture = "en-US",
            User = user,
            Chat = new Chat
            {
                Id = Guid.Empty,
                CharacterId = Guid.Empty,
            },
            Character = character,
            Actions = new[] { "action1", "action2" },
            Context = "some-context",
        };
        chat.AddMessage(user, "Hello");
        chat.AddMessage(character, "World");
        chat.AddMessage(user, "Question");
        
        var actual = _builder.BuildReplyPromptString(chat, 0, 4096);

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
        """.ReplaceLineEndings("\n")));
    }

    [Test]
    public void TestPromptMemory()
    {
        var character = new ChatSessionDataCharacter
        {
            Name = "Jane",
            Description = "some-description",
            Personality = "some-personality",
            Scenario = "some-scenario",
            FirstMessage = "some-first-message",
        };
        var user = new ChatSessionDataUser { Name = "Joe" };
        var chat = new ChatSessionData
        {
            Culture = "en-US",
            User = user,
            Chat = new Chat
            {
                Id = Guid.Empty,
                CharacterId = Guid.Empty,
            },
            Character = character,
        };
        chat.AddMessage(user, "Hello");
        chat.AddMessage(character, "World");
        chat.AddMessage(user, "Question");
        chat.Memories.Add(new ChatSessionDataMemory { Id = Guid.Empty, Text = "memory-1" });
        chat.Memories.Add(new ChatSessionDataMemory { Id = Guid.Empty, Text = "memory-2" });
        chat.Memories.Add(new ChatSessionDataMemory { Id = Guid.Empty, Text = "memory-3" });
        
        var actual = _builder.BuildReplyPromptString(chat, 1024, 4096);

        Assert.That(actual, Is.EqualTo("""
           This is a spoken conversation between Joe and Jane. You are playing the role of Jane. The current date and time is Saturday, February 3, 2001 4:05 AM. Keep the conversation flowing, actively engage with Joe. Stay in character. Emojis are prohibited, only use spoken words. Avoid making up facts about Joe.
           
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
           """.ReplaceLineEndings("\n")));
    }
    
    [Test]
    public void TestPromptActionInference()
    {
        var character = new ChatSessionDataCharacter
        {
            Name = "Jane",
            Description = "some-description",
            Personality = "some-personality",
            Scenario = "some-scenario",
            FirstMessage = "some-first-message",
            SystemPrompt = "some-system-prompt",
            PostHistoryInstructions = "some-post-history-instructions",
            MessageExamples = "Joe: Request\nJane: Response",
        };
        var user = new ChatSessionDataUser { Name = "Joe" };
        var chat = new ChatSessionData
        {
            Culture = "en-US",
            User = user,
            Chat = new Chat
            {
                Id = Guid.Empty,
                CharacterId = Guid.Empty,
            },
            Character = character,
            Actions = new[] { "action1", "action2" },
            Context = "some-context",
        };
        chat.AddMessage(user, "Hello");
        chat.AddMessage(character, "World");
        chat.AddMessage(user, "Question");
        var actual = _builder.BuildActionInferencePromptString(chat);

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
        
        Which of the following actions should be executed to match Jane's last message?
        - [action1]
        - [action2]

        Only write the action.

        Action: [
        """.ReplaceLineEndings("\n")));
    }
    
    [Test]
    public void TestPromptSummarization()
    {
        var character = new ChatSessionDataCharacter
        {
            Name = "Jane",
            Description = "some-description",
            Personality = "some-personality",
            Scenario = "some-scenario",
            FirstMessage = "some-first-message",
            SystemPrompt = "some-system-prompt",
            PostHistoryInstructions = "some-post-history-instructions",
            MessageExamples = "Joe: Request\nJane: Response",
        };
        var user = new ChatSessionDataUser { Name = "Joe" };
        var chat = new ChatSessionData
        {
            Culture = "en-US",
            User = user,
            Chat = new Chat
            {
                Id = Guid.Empty,
                CharacterId = Guid.Empty,
            },
            Character = character,
            Actions = new[] { "action1", "action2" },
            Context = "some-context",
        };
        chat.AddMessage(user, "Hello");
        chat.AddMessage(character, "World");
        chat.AddMessage(user, "Question");
        var actual = _builder.BuildSummarizationPromptString(chat, chat.Messages);

        Assert.That(actual, Is.EqualTo("""
           You must write facts about Jane and Joe from their conversation.
           Facts must be short. Be specific. Write in a way that identifies the user associated with the fact. Use words from the conversation when possible.
           Prefer facts about: physical descriptions, emotional state, relationship progression, gender, sexual orientation, preferences, events.
           Write the most useful facts first.

           Conversation:
           <START>
           Joe: Hello
           Jane: World
           Joe: Question
           <END>

           Facts learned:
           
           """.ReplaceLineEndings("\n")));
    }
}
