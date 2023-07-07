using System.Collections.Concurrent;
using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatSession : IAsyncDisposable
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ChatServices _services;
    private readonly ChatSessionData _chatSessionData;
    private readonly ClientStartChatMessage _startChatMessage;
    private readonly ChatTextProcessor _chatTextProcessor;
    private readonly bool _pauseSpeechRecognitionDuringPlayback;
    private readonly ILogger<UserConnection> _logger;
    private readonly ExclusiveLocalInputHandle? _inputHandle;
    private readonly ITemporaryFileCleanup _temporaryFileCleanup;
    private readonly ChatSessionState _state;

    /* NEW */
    
    private readonly BlockingCollection<IMessageProcessing> _messageQueue = new();
    private readonly Task _processMessagesTask;
    private readonly CancellationTokenSource _cts = new();
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly ILoggerFactory _loggerFactory;

    public void Enqueue(IMessageProcessing messageProcessing)
    {
        // Enqueue the message
        try
        {
            _messageQueue.Add(messageProcessing, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Log exception or handle it according to your needs
        }
    }

    public async Task CloseAsync()
    {
        // Cancel processing task
        _cts.Cancel();

        // Wait for processing task to finish after cancellation
        await _processMessagesTask;
    }

    private async Task ProcessMessages(CancellationToken token)
    {
        try
        {
            // BlockingCollection.GetConsumingEnumerable will block until a new item is available or
            // CompleteAdding is called
            foreach (var message in _messageQueue.GetConsumingEnumerable(token))
            {
                try
                {
                    await message.HandleAsync(token);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, "Error processing message {MessageType}", message.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _messageQueue.Dispose();
        }
    }
    
    /* /NEW */
    
    public ChatSession(IUserConnectionTunnel tunnel,
        ILoggerFactory loggerFactory,
        ChatServices services,
        ChatSessionData chatSessionData,
        ClientStartChatMessage startChatMessage,
        ChatTextProcessor chatTextProcessor,
        ProfileSettings profile,
        ExclusiveLocalInputHandle? inputHandle,
        ITemporaryFileCleanup temporaryFileCleanup,
        PendingSpeechManager pendingSpeech,
        ChatSessionState state)
    {
        _tunnel = tunnel;
        _services = services;
        _chatSessionData = chatSessionData;
        _startChatMessage = startChatMessage;
        _chatTextProcessor = chatTextProcessor;
        _pauseSpeechRecognitionDuringPlayback = profile.PauseSpeechRecognitionDuringPlayback;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<UserConnection>();
        _inputHandle = inputHandle;
        _temporaryFileCleanup = temporaryFileCleanup;
        _pendingSpeech = pendingSpeech;
        _state = state;

        _processMessagesTask = Task.Run(() => ProcessMessages(_cts.Token), _cts.Token);
    }
    
    public void HandleSpeechPlaybackComplete()
    {
        _state.SpeechComplete();
        _inputHandle?.RequestResumeSpeechRecognition();
    }

    public void SendReady()
    {
        Enqueue(new HandleReadyMessageProcessing(_tunnel, _loggerFactory, _chatSessionData, _services, _state, _startChatMessage, _pendingSpeech, _temporaryFileCleanup));
    }

    private void OnSpeechRecognitionStarted(object? sender, EventArgs e)
    {
        _logger.LogInformation("Speech recognition started");
        Enqueue(new ActionMessageProcessing(async _ =>
        {
            await _state.AbortReplyAsync();
            #warning Here we stopped speaking bool; instead, detect interruptions on the client.
            await _tunnel.SendAsync(new ServerSpeechRecognitionStartMessage(), CancellationToken.None);
        }));
    }

    private void OnSpeechRecognitionFinished(object? sender, string e)
    {
        _logger.LogInformation("Speech recognition finished: {Text}", e);
        if (_pauseSpeechRecognitionDuringPlayback) _inputHandle?.RequestPauseSpeechRecognition();
        Enqueue(new ActionMessageProcessing(async ct =>
        {
            await _tunnel.SendAsync(new ServerSpeechRecognitionEndMessage { Text = e }, CancellationToken.None);
        }));
        HandleClientMessage(new ClientSendMessage { Text = e });
    }

    public void HandleClientMessage(ClientSendMessage clientSendMessage)
    {
        Enqueue(new HandleClientMessageProcessing(_tunnel, _loggerFactory, _inputHandle, _pauseSpeechRecognitionDuringPlayback, _chatSessionData, _services, clientSendMessage, _state, _chatTextProcessor, _state, _startChatMessage, _pendingSpeech, _temporaryFileCleanup));
    }

    public async ValueTask DisposeAsync()
    {
        if (_inputHandle != null)
        {
            _inputHandle.SpeechRecognitionStarted -= OnSpeechRecognitionStarted;
            _inputHandle.SpeechRecognitionFinished -= OnSpeechRecognitionFinished;
            _inputHandle.Dispose();
        }

        await CloseAsync();
    }
}