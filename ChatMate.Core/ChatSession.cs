using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public interface IChatSession : IAsyncDisposable
{
    void SendReady();
    void HandleClientMessage(ClientSendMessage clientSendMessage);
    void HandleSpeechPlaybackStart(double duration);
    void HandleSpeechPlaybackComplete();
}

public sealed partial class ChatSession : IChatSession
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly ITextGenService _textGen;
    private readonly ChatSessionData _chatSessionData;
    private readonly IChatTextProcessor _chatTextProcessor;
    private readonly bool _pauseSpeechRecognitionDuringPlayback;
    private readonly ILogger<UserConnection> _logger;
    private readonly ExclusiveLocalInputHandle? _inputHandle;
    private readonly ChatSessionState _chatSessionState;
    private readonly ISpeechGenerator _speechGenerator;
    private readonly IAnimationSelectionService? _animationSelection;

    public ChatSession(IUserConnectionTunnel tunnel,
        ILoggerFactory loggerFactory,
        IPerformanceMetrics performanceMetrics,
        ITextGenService textGen,
        ChatSessionData chatSessionData,
        IChatTextProcessor chatTextProcessor,
        ProfileSettings profile,
        ExclusiveLocalInputHandle? inputHandle,
        ChatSessionState chatSessionState,
        ISpeechGenerator speechGenerator,
        IAnimationSelectionService? animationSelection)
    {
        _tunnel = tunnel;
        _performanceMetrics = performanceMetrics;
        _textGen = textGen;
        _chatSessionData = chatSessionData;
        _chatTextProcessor = chatTextProcessor;
        _pauseSpeechRecognitionDuringPlayback = profile.PauseSpeechRecognitionDuringPlayback;
        _logger = loggerFactory.CreateLogger<UserConnection>();
        _inputHandle = inputHandle;
        _chatSessionState = chatSessionState;
        _speechGenerator = speechGenerator;
        _animationSelection = animationSelection;

        if (_inputHandle != null)
        {
            _inputHandle.SpeechRecognitionStarted += OnSpeechRecognitionStarted;
            _inputHandle.SpeechRecognitionFinished += OnSpeechRecognitionFinished;
        }

        _messageQueueProcessTask = Task.Run(() => ProcessQueueAsync(_messageQueueCancellationTokenSource.Token), _messageQueueCancellationTokenSource.Token);
    }
    
    public void HandleSpeechPlaybackStart(double duration)
    {
        _chatSessionState.StartSpeechAudio(duration);
    }

    public void HandleSpeechPlaybackComplete()
    {
        _chatSessionState.StopSpeechAudio();
        _inputHandle?.RequestResumeSpeechRecognition();
    }

    private void OnSpeechRecognitionStarted(object? sender, EventArgs e)
    {
        _logger.LogInformation("Speech recognition started");
        Enqueue(async ct =>
        {
            await _chatSessionState.AbortGeneratingReplyAsync();
            await _tunnel.SendAsync(new ServerSpeechRecognitionStartMessage(), ct);
        });
    }

    private void OnSpeechRecognitionFinished(object? sender, string e)
    {
        _logger.LogInformation("Speech recognition finished: {Text}", e);
        if (_pauseSpeechRecognitionDuringPlayback) _inputHandle?.RequestPauseSpeechRecognition();
        Enqueue(async ct =>
        {
            await _tunnel.SendAsync(new ServerSpeechRecognitionEndMessage
            {
                Text = e
            }, ct);
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