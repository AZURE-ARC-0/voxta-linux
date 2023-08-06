using Moq;
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
        _provider = new SimpleMemoryProvider(_repository.Object);
    }

    [Test]
    public void SimpleMemory()
    {
        var books = new[]
        {
            new MemoryBook
            {
                Id = Guid.Empty,
                CharacterId = Guid.Empty,
                ChatId = Guid.Empty,
                Items = new List<MemoryItem>
                {
                    new()
                    {
                        Id = Guid.Empty,
                        Weight = 0,
                        Keywords = new[] { "apple" },
                        Value = "John likes apples"
                    }
                }
            }
        };
        _repository.Setup(mock => mock.GetScopeMemoryBooksAsync(It.IsAny<Guid>(),It.IsAny<CancellationToken>())).ReturnsAsync(books);

        _provider.QueryMemoryFast(_chatSessionData, _chatSessionData.Memories);

        Assert.That(_chatSessionData.Memories.Select(m => m.Value), Is.EqualTo(new[]
        {
            "John likes apples"
        }));
    }
}