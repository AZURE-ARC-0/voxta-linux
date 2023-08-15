using Moq;
using NUnit.Framework;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;

namespace Voxta.Shared.LLMUtils.Tests;

public class Llama2PromptBuilderTests
{
    private TextPromptBuilder _builder = null!;

    [SetUp]
    public void Setup()
    {
        var tokenizer = new AverageTokenizer();
        var timeProvider = new Mock<ITimeProvider>();
        timeProvider.Setup(m => m.LocalNow).Returns(new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero));
        _builder = new Llama2PromptBuilder(tokenizer, timeProvider.Object);
    }

    [Test]
    public void TestPromptMinimal()
    {
        var character = new ChatSessionDataCharacter
        {
            Name = "Jane",
            Description = "",
            Personality = "",
            Scenario = "",
            SystemPrompt = "system_prompt"
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
        chat.AddMessage(character, "model_answer_1");
        chat.AddMessage(user, "user_msg_1");
        chat.AddMessage(character, "model_answer_2");
        chat.AddMessage(user, "user_msg_2");
        
        var actual = _builder.BuildReplyPromptString(chat, 0, 4096);

        Assert.That(actual, Is.EqualTo("""
           <s>[INST] <<SYS>>
           system_prompt
           
           Only write a single reply from Jane for natural speech.
           <</SYS>>

           [/INST] model_answer_1 </s><s>[INST] user_msg_1 [/INST] model_answer_2 </s><s>[INST] user_msg_2 [/INST]
           """.ReplaceLineEndings("\n")));
    }
}