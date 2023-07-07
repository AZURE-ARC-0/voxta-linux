using ChatMate.Abstractions.Management;
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
    private Mock<ITextToSpeechService> _textToSpeech = null!;
    private List<string> _serverErrors = null!;

    [SetUp]
    public void Setup()
    {
        _serverErrors = new List<string>();
        
        _tunnelMock = new Mock<IUserConnectionTunnel>();
        _textGen = new Mock<ITextGenService>();
        _textToSpeech = new Mock<ITextToSpeechService>();
        _chatSessionData = new ChatSessionData
        {
            UserName = "User",
            BotName = "Bot",
            Preamble = new TextData
            {
                Text = "Preamble",
                Tokens = 1,
            },
            AudioPath = "/audio-path",
            TtsVoice = "voice",
        };
        var chatTextProcessor = new Mock<IChatTextProcessor>();
        var profile = new ProfileSettings
        {
            Name = "User",
            Description = "User Description",
            EnableSpeechRecognition = true,
            PauseSpeechRecognitionDuringPlayback = false,
        };
        var inputManager = new ExclusiveLocalInputManager();
        var inputHandle = new ExclusiveLocalInputHandle(inputManager);
        var temporaryFileCleanup = new Mock<ITemporaryFileCleanup>();
        var pendingSpeech = new PendingSpeechManager();
        var chatSessionState = new ChatSessionState();

        _session = new ChatSession(
            _tunnelMock.Object,
            new NullLoggerFactory(),
            new ChatServices
            {
                TextGen = _textGen.Object,
                TextToSpeech = _textToSpeech.Object,
            },
            _chatSessionData,
            chatTextProcessor.Object,
            profile,
            inputHandle,
            temporaryFileCleanup.Object,
            pendingSpeech,
            chatSessionState
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
        _textToSpeech.Setup(m => m.GenerateSpeechAsync(It.IsAny<SpeechRequest>(), It.IsAny<ISpeechTunnel>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReplyMessage>(), It.IsAny<CancellationToken>())).Verifiable();
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerSpeechMessage>(), It.IsAny<CancellationToken>())).Verifiable();

        _session.HandleClientMessage(new ClientSendMessage { Text = "Ping!" });

        await AssertSession();
        Assert.Multiple(() =>
        {
            Assert.That(_tunnelMock.Invocations[0].Arguments.OfType<ServerReplyMessage>().FirstOrDefault()?.Text, Is.EqualTo("Pong!"));
            Assert.That(_tunnelMock.Invocations[1].Arguments.OfType<ServerSpeechMessage>().FirstOrDefault()?.Url, Is.Not.Null.Or.Empty);
        });
    }

    private async Task AssertSession()
    {
        await _session.WaitForPendingQueueItemsAsync();
        Assert.That(_serverErrors, Is.Empty);
        _tunnelMock.Verify();
    }
}