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

    [SetUp]
    public void Setup()
    {
        _tunnelMock = new Mock<IUserConnectionTunnel>();
        var services = new ChatServices
        {
            TextGen = Mock.Of<ITextGenService>(),
            TextToSpeech = Mock.Of<ITextToSpeechService>()
        };
        var chatSessionData = new ChatSessionData
        {
            UserName = "User",
            BotName = "Bot",
            Preamble = new TextData
            {
                Text = "Preamble",
                Tokens = 1,
            }
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
        
        _session = new ChatSession(_tunnelMock.Object, new NullLoggerFactory(), services, chatSessionData, chatTextProcessor.Object, profile, inputHandle, temporaryFileCleanup.Object, pendingSpeech, chatSessionState);
    }
    
    [TearDown]
    public void TearDown()
    {
        _session.DisposeAsync().AsTask().Wait();
    }

    [Test]
    public async Task TestSendReady()
    {
        _tunnelMock.Setup(m => m.SendAsync(It.IsAny<ServerReadyMessage>(), It.IsAny<CancellationToken>())).Verifiable();
        
        _session.SendReady();
        await _session.WaitForPendingQueueItemsAsync();
        
        _tunnelMock.VerifyAll();
    }
}