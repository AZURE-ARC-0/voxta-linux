using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChatMate.Core.Tests;

public class ChatSessionTests
{
    private ChatSession _session = null!;
    private Mock<IUserConnectionTunnel> _tunnelMock = null!;
    private ChatSessionData _chatSessionData = null!;
    private Mock<ITextGenService> _textGen = null!;
    private List<string> _serverErrors = null!;
    private Mock<ISpeechGenerator> _speechGenerator = null!;

    [SetUp]
    public void Setup()
    {
        _serverErrors = new List<string>();
        
        _tunnelMock = new Mock<IUserConnectionTunnel>();
        _textGen = new Mock<ITextGenService>();
        _chatSessionData = new ChatSessionData
        {
            UserName = "User",
            Character = new Character
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Assistant",
                SystemPrompt = "You are a test assistant",
                Description = "",
                Personality = "",
                Scenario = "This is a test",
                Services = null!
            },
            AudioPath = "/audio-path",
            TtsVoice = "voice",
        };
        var chatTextProcessor = new Mock<IChatTextProcessor>();
        chatTextProcessor.Setup(m => m.ProcessText(It.IsAny<string?>())).Returns<string?>(text => text ?? "");
        var profile = new ProfileSettings
        {
            Name = "User",
            Description = "User Description",
            EnableSpeechRecognition = true,
            PauseSpeechRecognitionDuringPlayback = false,
        };
        var inputManager = new ExclusiveLocalInputManager();
        var inputHandle = new ExclusiveLocalInputHandle(inputManager);
        var chatSessionState = new ChatSessionState();
        _speechGenerator = new Mock<ISpeechGenerator>();

        _session = new ChatSession(
            _tunnelMock.Object,
            new NullLoggerFactory(),
            Mock.Of<IPerformanceMetrics>(),
            _textGen.Object,
            _chatSessionData,
            chatTextProcessor.Object,
            profile,
            inputHandle,
            chatSessionState,
            _speechGenerator.Object,
            null
        );

        _tunnelMock
            .Setup(m => m.SendAsync(It.IsAny<ServerErrorMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServerErrorMessage, CancellationToken>((msg, _) => _serverErrors.Add(msg.Message));
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
        
        _session.SendReady();
        
        await AssertSession();
    }
    
    [Test]
    public async Task TestSendReady_WithGreeting()
    {
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReadyMessage>(), It.IsAny<CancellationToken>())).Verifiable();
        
        _session.SendReady();
        
        await AssertSession();
        Assert.Inconclusive("Does not actually test the greeting message.");
    }

    [Test]
    public async Task TestHandleClientMessage()
    {
        _textGen.Setup(m => m.GenerateReplyAsync(It.IsAny<IReadOnlyChatSessionData>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TextData { Text = "Pong!" });
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
        _textGen.Setup(m => m.GenerateReplyAsync(It.IsAny<IReadOnlyChatSessionData>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => new TextData { Text = genIdx++ == 0 ? "This speech will be interrupted." : "How rude!" });
        _speechGenerator.Setup(m => m.CreateSpeechAsync(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>())).ReturnsAsync((string?) null);
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReplyMessage>(), It.IsAny<CancellationToken>())).Verifiable();

        _session.HandleClientMessage(new ClientSendMessage { Text = "Hello!" });
        await _session.WaitForPendingQueueItemsAsync();
        _session.HandleSpeechPlaybackStart(1);
        await Task.Delay(500);
        _session.HandleClientMessage(new ClientSendMessage { Text = "Stop!" });

        await AssertSession();
        Assert.Multiple(() =>
        {
            Assert.That(_chatSessionData.GetMessagesAsString(), Is.EqualTo("""
                User: Hello!
                Assistant: This speech will...
                User: *interrupts {{char}}* Stop!
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
            .Setup(m => m.GenerateReplyAsync(It.IsAny<IReadOnlyChatSessionData>(), It.IsAny<CancellationToken>()))
            .Returns<IReadOnlyChatSessionData, CancellationToken>(async (_, ct) =>
            {
                if (genIdx++ == 0) await Task.Delay(10000, ct);
                return new TextData { Text = $"Pong {genIdx}!" };
            });
        _speechGenerator.Setup(m => m.CreateSpeechAsync(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>())).ReturnsAsync((string?) null);
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReplyMessage>(), It.IsAny<CancellationToken>())).Verifiable();

        _session.HandleClientMessage(new ClientSendMessage { Text = "Ping 1!" });
        _session.HandleClientMessage(new ClientSendMessage { Text = "Ping 2!" });

        await AssertSession();
        Assert.Multiple(() =>
        {
            Assert.That(_chatSessionData.GetMessagesAsString(), Is.EqualTo("""
                User: Ping 1!
                Ping 2!
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