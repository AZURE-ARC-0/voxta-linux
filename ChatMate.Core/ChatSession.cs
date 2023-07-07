using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public interface IChatSession : IAsyncDisposable
{
    void SendReady();
    void HandleClientMessage(ClientSendMessage clientSendMessage);
    void HandleSpeechPlaybackComplete();
}

public sealed partial class ChatSession : IChatSession
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ChatServices _services;
    private readonly ChatSessionData _chatSessionData;
    private readonly IChatTextProcessor _chatTextProcessor;
    private readonly bool _pauseSpeechRecognitionDuringPlayback;
    private readonly ILogger<UserConnection> _logger;
    private readonly ExclusiveLocalInputHandle? _inputHandle;
    private readonly ITemporaryFileCleanup _temporaryFileCleanup;
    private readonly ChatSessionState _chatSessionState;
    private readonly PendingSpeechManager _pendingSpeech;

    public ChatSession(IUserConnectionTunnel tunnel,
        ILoggerFactory loggerFactory,
        ChatServices services,
        ChatSessionData chatSessionData,
        IChatTextProcessor chatTextProcessor,
        ProfileSettings profile,
        ExclusiveLocalInputHandle? inputHandle,
        ITemporaryFileCleanup temporaryFileCleanup,
        PendingSpeechManager pendingSpeech,
        ChatSessionState chatSessionState)
    {
        _tunnel = tunnel;
        _services = services;
        _chatSessionData = chatSessionData;
        _chatTextProcessor = chatTextProcessor;
        _pauseSpeechRecognitionDuringPlayback = profile.PauseSpeechRecognitionDuringPlayback;
        _logger = loggerFactory.CreateLogger<UserConnection>();
        _inputHandle = inputHandle;
        _temporaryFileCleanup = temporaryFileCleanup;
        _pendingSpeech = pendingSpeech;
        _chatSessionState = chatSessionState;

        if (_inputHandle != null)
        {
            _inputHandle.SpeechRecognitionStarted += OnSpeechRecognitionStarted;
            _inputHandle.SpeechRecognitionFinished += OnSpeechRecognitionFinished;
        }

        _messageQueueProcessTask = Task.Run(() => ProcessQueueAsync(_messageQueueCancellationTokenSource.Token), _messageQueueCancellationTokenSource.Token);
    }

    public void HandleSpeechPlaybackComplete()
    {
        _chatSessionState.SpeechComplete();
        _inputHandle?.RequestResumeSpeechRecognition();
    }

    private void OnSpeechRecognitionStarted(object? sender, EventArgs e)
    {
        _logger.LogInformation("Speech recognition started");
        Enqueue(async ct =>
        {
            await _chatSessionState.AbortReplyAsync();
            await _tunnel.SendAsync(new ServerSpeechRecognitionStartMessage(), ct);
        });
    }

    private void OnSpeechRecognitionFinished(object? sender, string e)
    {
        _logger.LogInformation("Speech recognition finished: {Text}", e);
        if (_pauseSpeechRecognitionDuringPlayback) _inputHandle?.RequestPauseSpeechRecognition();
        Enqueue(async ct =>
        {
            await _tunnel.SendAsync(new ServerSpeechRecognitionEndMessage { Text = e }, ct);
        });
        HandleClientMessage(new ClientSendMessage { Text = e });
    }

    public async ValueTask DisposeAsync()
    {
        if (_inputHandle != null)
        {
            _inputHandle.SpeechRecognitionStarted -= OnSpeechRecognitionStarted;
            _inputHandle.SpeechRecognitionFinished -= OnSpeechRecognitionFinished;
            _inputHandle.Dispose();
        }

        await StopProcessingQueue();
    }
}