using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Voxta.Abstractions.Repositories;

namespace Voxta.Core;

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
    private readonly ChatSessionState _chatSessionState;
    private readonly ISpeechGenerator _speechGenerator;
    private readonly IActionInferenceService? _actionInference;
    private readonly ISpeechToTextService? _speechToText;
    private readonly IChatRepository _chatRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly ISanitizer _sanitizer = new Sanitizer();

    public ChatSession(IUserConnectionTunnel tunnel,
        ILoggerFactory loggerFactory,
        IPerformanceMetrics performanceMetrics,
        ITextGenService textGen,
        ChatSessionData chatSessionData,
        IChatTextProcessor chatTextProcessor,
        ProfileSettings profile,
        ChatSessionState chatSessionState,
        ISpeechGenerator speechGenerator,
        IActionInferenceService? actionInference,
        ISpeechToTextService? speechToText,
        IChatRepository chatRepository,
        IChatMessageRepository chatMessageRepository
        )
    {
        _tunnel = tunnel;
        _performanceMetrics = performanceMetrics;
        _textGen = textGen;
        _chatSessionData = chatSessionData;
        _chatTextProcessor = chatTextProcessor;
        _pauseSpeechRecognitionDuringPlayback = profile.PauseSpeechRecognitionDuringPlayback;
        _logger = loggerFactory.CreateLogger<UserConnection>();
        _chatSessionState = chatSessionState;
        _speechGenerator = speechGenerator;
        _actionInference = actionInference;
        _speechToText = speechToText;
        _chatRepository = chatRepository;
        _chatMessageRepository = chatMessageRepository;

        if (speechToText != null)
        {
            speechToText.SpeechRecognitionStarted += OnSpeechRecognitionStarted;
            speechToText.SpeechRecognitionPartial += OnSpeechRecognitionPartial;
            speechToText.SpeechRecognitionFinished += OnSpeechRecognitionFinished;
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
        _speechToText?.StartMicrophoneTranscription();
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
    
    private void OnSpeechRecognitionPartial(object? sender, string e)
    {
        Enqueue(async ct =>
        {
            await _tunnel.SendAsync(new ServerSpeechRecognitionPartialMessage
            {
                Text = e
            }, ct);
        });
    }

    private void OnSpeechRecognitionFinished(object? sender, string e)
    {
        _logger.LogInformation("Speech recognition finished: {Text}", e);
        if (_pauseSpeechRecognitionDuringPlayback) _speechToText?.StopMicrophoneTranscription();
        Enqueue(async ct =>
        {
            await _tunnel.SendAsync(new ServerSpeechRecognitionEndMessage
            {
                Text = e
            }, ct);
        });
    }

    public async ValueTask DisposeAsync()
    {
        _messageQueueCancellationTokenSource.Cancel();
        await _messageQueueProcessTask;
        
        if (_speechToText != null)
        {
            _speechToText.SpeechRecognitionStarted -= OnSpeechRecognitionStarted;
            _speechToText.SpeechRecognitionPartial -= OnSpeechRecognitionPartial;
            _speechToText.SpeechRecognitionFinished -= OnSpeechRecognitionFinished;
        }

        _speechToText?.Dispose();
        _textGen.Dispose();
        _speechGenerator.Dispose();
        _actionInference?.Dispose();
        _processingSemaphore.Dispose();
        _messageQueueCancellationTokenSource.Dispose();
    }
}