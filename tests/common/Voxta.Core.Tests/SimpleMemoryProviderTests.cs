using Moq;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;

namespace Voxta.Core.Tests;

public class SimpleMemoryProviderTests
{
    private Mock<IMemoryRepository> _repository = null!;
    private SimpleMemoryProvider _provider = null!;
    private ChatSessionData _chatSessionData = null!;

    [SetUp]
    public void SetUp()
    {
        _chatSessionData = new ChatSessionData
        {
            UserName = "User",
            Chat = new Chat
            {
                Id = Guid.Empty,
                CharacterId = Guid.Empty,
            },
            Character = new CharacterCardExtended
            {
                Name = "Assistant",
                SystemPrompt = "You are a test assistant",
                Description = "",
                Personality = "",
                Scenario = "This is a test",
                FirstMessage = "Ready.",
                Services = new CharacterServicesMap()
            },
            AudioPath = "/audio-path",
            
        };
        _repository = new Mock<IMemoryRepository>();
        var performanceMetrics = new Mock<IPerformanceMetrics>();
        performanceMetrics.Setup(m => m.Start(It.IsAny<string>())).Returns(Mock.Of<IPerformanceMetricsTracker>());
        _provider = new SimpleMemoryProvider(_repository.Object, performanceMetrics.Object);
    }

    [Test]
    public async Task MatchMemoryByWord()
    {
        _chatSessionData.AddMessage(_chatSessionData.UserName, "I like apples.");
        await ArrangeMemoryBooksAsync(
            (Keywords: new[] { "apples" }, Value: "Assistant likes apples", Weight: 0)
        );
        
        _provider.QueryMemoryFast(_chatSessionData, _chatSessionData.Memories);

        AssertMemories(
            "Assistant likes apples"
        );
    }
    
    [Test]
    public async Task RecentMessagesGetPriority()
    {
        _chatSessionData.AddMessage(_chatSessionData.UserName, "I like apples.");
        _chatSessionData.AddMessage(_chatSessionData.Character.Name, "What else do you like?");
        _chatSessionData.AddMessage(_chatSessionData.UserName, "I really like hugs!");
        await ArrangeMemoryBooksAsync(
            (Keywords: new[] { "apples", "fruit" }, Value: "Assistant likes apples", Weight: 0),
            (Keywords: new[] { "affection", "hugs", "hug" }, Value: "Assistant is afraid of physical contact", Weight: 0)
        );
        
        _provider.QueryMemoryFast(_chatSessionData, _chatSessionData.Memories);

        AssertMemories(
            "Assistant is afraid of physical contact",
            "Assistant likes apples"
        );
    }
    
    [Test]
    public async Task MatchesAreReordered()
    {
        _chatSessionData.AddMessage(_chatSessionData.UserName, "You want to play chess?");
        await ArrangeMemoryBooksAsync(
            (Keywords: new[] { "play" }, Value: "Assistant loves playing games", Weight: 0),
            (Keywords: new[] { "chess" }, Value: "Assistant can barely play chess", Weight: 0),
            (Keywords: new[] { "talk" }, Value: "Assistant likes listening more than speaking", Weight: 0)
        );
        
        _provider.QueryMemoryFast(_chatSessionData, _chatSessionData.Memories);

        AssertMemories(
            "Assistant can barely play chess",
            "Assistant loves playing games"
        );
        
        _chatSessionData.AddMessage(_chatSessionData.Character.Name, "I'd love to, by I can barely play!");
        _chatSessionData.AddMessage(_chatSessionData.UserName, "Do you want to talk about it?");

        _provider.QueryMemoryFast(_chatSessionData, _chatSessionData.Memories);
    
        AssertMemories(
            "Assistant likes listening more than speaking",
            "Assistant loves playing games",
            "Assistant can barely play chess"
        );
    }

    private async Task ArrangeMemoryBooksAsync(params (string[] Keywords, string Value, int Weight)[] memories)
    {
        var books = new[]
        {
            new MemoryBook
            {
                Id = Guid.Empty,
                CharacterId = Guid.Empty,
                ChatId = Guid.Empty,
                Items = memories.Select(x =>
                    new MemoryItem
                    {
                        Id = Guid.Empty,
                        Weight = x.Weight,
                        Keywords = x.Keywords,
                        Text = x.Value
                    }
                ).ToList()
            }
        };
        _repository.Setup(mock => mock.GetScopeMemoryBooksAsync(It.IsAny<Guid>(),It.IsAny<CancellationToken>())).ReturnsAsync(books);
        await _provider.Initialize(Guid.Empty, _chatSessionData, CancellationToken.None);
    }

    private void AssertMemories(params string[] expected)
    {
        Assert.That(_chatSessionData.Memories.Select(m => m.Text).ToArray(), Is.EqualTo(expected));
    }
}
