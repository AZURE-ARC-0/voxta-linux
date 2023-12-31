using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.System;

namespace Voxta.Core.Tests;

public class ChatSessionTests
{
    private ChatSession _session = null!;
    private Mock<IUserConnectionTunnel> _tunnelMock = null!;
    private ChatSessionData _chatSessionData = null!;
    private Mock<ITextGenService> _textGen = null!;
    private List<string> _serverErrors = null!;
    private Mock<ISpeechGenerator> _speechGenerator = null!;
    private Mock<ITimeProvider> _timeProvider = null!;

    [SetUp]
    public void Setup()
    {
        _serverErrors = new List<string>();
        _tunnelMock = new Mock<IUserConnectionTunnel>();
        _timeProvider = new Mock<ITimeProvider>();
        
        _textGen = new Mock<ITextGenService>();
        _textGen.Setup(m => m.SettingsRef).Returns(new ServiceSettingsRef { ServiceName = "Test", ServiceId = Guid.Empty });
        var summarization = new Mock<ISummarizationService>();
        summarization.Setup(m => m.SettingsRef).Returns(new ServiceSettingsRef { ServiceName = "Test", ServiceId = Guid.Empty });
        _speechGenerator = new Mock<ISpeechGenerator>();

        _chatSessionData = new ChatSessionData
        {
            Culture = "en-US",
            User = new ChatSessionDataUser { Name = "User" },
            Chat = new Chat
            {
                Id = Guid.NewGuid(),
                CharacterId = Guid.NewGuid(),
            },
            Character = new ChatSessionDataCharacter
            {
                Name = "Assistant",
                SystemPrompt = "You are a test assistant",
                Description = "",
                Personality = "",
                Scenario = "This is a test",
                FirstMessage = "Ready.",
            },
            AudioPath = "/audio-path",
        };
        
        var chatTextProcessor = new Mock<IChatTextProcessor>();
        chatTextProcessor.Setup(m => m.ProcessText(It.IsAny<string?>())).Returns((string? text) => text ?? "");
        
        var profile = new ProfileSettings
        {
            Name = "User",
            Description = "User Description",
            PauseSpeechRecognitionDuringPlayback = false,
        };

        var chatSessionState = new ChatSessionState(_timeProvider.Object);
        var chatRepository = new Mock<IChatRepository>();
        var chatMessageRepository = new Mock<IChatMessageRepository>();
        var performanceMetrics = new Mock<IPerformanceMetrics>();
        performanceMetrics.Setup(m => m.Start(It.IsAny<string>())).Returns(Mock.Of<IPerformanceMetricsTracker>());
        var memoryProvider = new Mock<IMemoryProvider>();
        memoryProvider.Setup(m => m.QueryMemoryFast(It.IsAny<ChatSessionData>()));
        
        _session = new ChatSession(
            _tunnelMock.Object,
            new NullLoggerFactory(),
            performanceMetrics.Object,
            _textGen.Object,
            _chatSessionData,
            chatTextProcessor.Object,
            profile,
            chatSessionState,
            _speechGenerator.Object,
            null,
            null,
            summarization.Object,
            chatRepository.Object,
            chatMessageRepository.Object,
            memoryProvider.Object
        );

        _tunnelMock
            .Setup(m => m.SendAsync(It.IsAny<ServerErrorMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServerErrorMessage, CancellationToken>((msg, _) => _serverErrors.Add(msg.Details ?? msg.Message));
    }
    
    [TearDown]
    public void TearDown()
    {
        _session.DisposeAsync().AsTask().Wait();
    }

    [Test]
    public async Task TestSendReady_NoGreeting()
    {
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReadyMessage>(), It.IsAny<CancellationToken>())).Verifiable();
        
        _session.HandleStartChat();
        
        await AssertSession();
    }
    
    [Test]
    public async Task TestSendReady_WithGreeting()
    {
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReadyMessage>(), It.IsAny<CancellationToken>())).Verifiable();
        
        _session.HandleStartChat();
        
        await AssertSession();
        Assert.Inconclusive("Does not actually test the greeting message.");
    }

    [Test]
    public async Task TestHandleClientMessage()
    {
        _textGen.Setup(m => m.GenerateReplyAsync(It.IsAny<IChatInferenceData>(), It.IsAny<CancellationToken>())).ReturnsAsync("Pong!");
        _speechGenerator.Setup(m => m.CreateSpeechAsync("Pong!", It.IsAny<string>(), false, It.IsAny<CancellationToken>())).ReturnsAsync("/audio-path");
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReplyMessage>(), It.IsAny<CancellationToken>())).Verifiable();
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerSpeechMessage>(), It.IsAny<CancellationToken>())).Verifiable();

        _session.HandleClientMessage(new ClientSendMessage { Text = "Ping!" });

        await AssertSession();
        Assert.Multiple(() =>
        {
            Assert.That(_tunnelMock.Invocations[0].Arguments.OfType<ServerReplyMessage>().FirstOrDefault()?.Text, Is.EqualTo("Pong!"));
            Assert.That(_tunnelMock.Invocations[1].Arguments.OfType<ServerSpeechMessage>().FirstOrDefault()?.Url, Is.EqualTo("/audio-path"));
        });
    }

    [Test]
    public async Task TestHandleClientMessage_InterruptSpeech()
    {
        var genIdx = 0;
        _textGen.Setup(m => m.GenerateReplyAsync(It.IsAny<IChatInferenceData>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => genIdx++ == 0 ? "This speech will be interrupted." : "How rude!");
        _speechGenerator.Setup(m => m.CreateSpeechAsync(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>())).ReturnsAsync((string?) null);
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReplyMessage>(), It.IsAny<CancellationToken>())).Verifiable();

        _timeProvider.Setup(m => m.UtcNow).Returns(new DateTimeOffset(2000, 1, 1, 0, 0, 0, 0, TimeSpan.Zero));
        _session.HandleClientMessage(new ClientSendMessage { Text = "Hello!" });
        _session.HandleSpeechPlaybackStart(1);
        _timeProvider.Setup(m => m.UtcNow).Returns(new DateTimeOffset(2000, 1, 1, 0, 0, 0, 500, TimeSpan.Zero));
        _session.HandleClientMessage(new ClientSendMessage { Text = "Stop!" });
        await _session.WaitForPendingQueueItemsAsync();

        await AssertSession();
        Assert.Multiple(() =>
        {
            Assert.That(_chatSessionData.GetMessagesAsString(), Is.EqualTo("""
                User: Hello!
                Assistant: This speech will...
                User: [interrupts Assistant] Stop!
                Assistant: How rude!
                """.ReplaceLineEndings("\n")));
            Assert.That(_tunnelMock.Invocations[0].Arguments.OfType<ServerReplyMessage>().FirstOrDefault()?.Text, Is.EqualTo("This speech will be interrupted."));
            Assert.That(_tunnelMock.Invocations[1].Arguments.OfType<ServerReplyMessage>().FirstOrDefault()?.Text, Is.EqualTo("How rude!"));
        });
    }
    
    [Test]
    public async Task TestHandleClientMessage_InterruptGeneration()
    {
        var genIdx = 0;
        _textGen
            .Setup(m => m.GenerateReplyAsync(It.IsAny<IChatInferenceData>(), It.IsAny<CancellationToken>()))
            .Returns<IChatInferenceData, CancellationToken>(async (_, ct) =>
            {
                if (genIdx++ == 0) await Task.Delay(10000, ct);
                return $"Pong {genIdx}!";
            });
        _speechGenerator.Setup(m => m.CreateSpeechAsync(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>())).ReturnsAsync((string?) null);
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReplyMessage>(), It.IsAny<CancellationToken>())).Verifiable();

        _session.HandleClientMessage(new ClientSendMessage { Text = "Ping 1!" });
        await Task.Delay(10);
        _session.HandleClientMessage(new ClientSendMessage { Text = "Ping 2!" });

        await AssertSession();
        Assert.Multiple(() =>
        {
            Assert.That(_chatSessionData.GetMessagesAsString(), Is.EqualTo("""
                User: Ping 1!; Ping 2!
                Assistant: Pong 2!
                """.ReplaceLineEndings("\n")));
            Assert.That(_tunnelMock.Invocations[0].Arguments.OfType<ServerReplyMessage>().FirstOrDefault()?.Text, Is.EqualTo("Pong 2!"));
        });
    }

    private async Task AssertSession()
    {
        await _session.WaitForPendingQueueItemsAsync();
        Assert.That(_serverErrors, Is.Empty);
        _tunnelMock.Verify();
    }
}