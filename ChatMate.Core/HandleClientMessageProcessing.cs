using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class HandleClientMessageProcessing : ReplyMessageProcessingBase, IMessageProcessing
{
    private readonly ILogger<HandleClientMessageProcessing> _logger;
    private readonly ExclusiveLocalInputHandle? _inputHandle;
    private readonly bool _pauseSpeechRecognitionDuringPlayback;
    private readonly ChatSessionData _chatSessionData;
    private readonly ChatServices _services;
    private readonly ClientSendMessage _clientSendMessage;
    private readonly ChatSessionState _state;
    private readonly ChatTextProcessor _chatTextProcessor;

    public HandleClientMessageProcessing(IUserConnectionTunnel tunnel, ILoggerFactory loggerFactory, ExclusiveLocalInputHandle? inputHandle, bool pauseSpeechRecognitionDuringPlayback,
        ChatSessionData chatSessionData, ChatServices services, ClientSendMessage clientSendMessage, ChatSessionState state, ChatTextProcessor chatTextProcessor,
        ChatSessionState chatSessionState, ClientStartChatMessage startChatMessage, PendingSpeechManager pendingSpeech, ITemporaryFileCleanup temporaryFileCleanup)
        : base(tunnel, chatSessionData, chatSessionState, startChatMessage, services, pendingSpeech, temporaryFileCleanup)
    {
        _logger = loggerFactory.CreateLogger<HandleClientMessageProcessing>();
        _inputHandle = inputHandle;
        _pauseSpeechRecognitionDuringPlayback = pauseSpeechRecognitionDuringPlayback;
        _chatSessionData = chatSessionData;
        _services = services;
        _clientSendMessage = clientSendMessage;
        _state = state;
        _chatTextProcessor = chatTextProcessor;
    }

    public async ValueTask HandleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received chat message: {Text}", _clientSendMessage.Text);
#warning This should actually happen once we have the text and sent the wav back
        if (_pauseSpeechRecognitionDuringPlayback) _inputHandle?.RequestPauseSpeechRecognition();

        var text = _clientSendMessage.Text;

        if (await _state.AbortReplyAsync())
        {
#warning Refactor this, find a cleaner way to do that (e.g. estimate the audio length cutoff?)
            var lastBotMessage = _chatSessionData.Messages.LastOrDefault(m => m.User == _chatSessionData.BotName);
            if (lastBotMessage != null)
            {
                lastBotMessage.Text = lastBotMessage.Text[..(lastBotMessage.Text.Length / 2)] + "...";
                lastBotMessage.Tokens = _services.TextGen.GetTokenCount(lastBotMessage.Text);
                _logger.LogInformation("Cutoff last bot message to account for the interruption: {Text}", lastBotMessage.Text);
            }
            text = "*interrupts {{Bot}}* " + text;
            _logger.LogInformation("Added interruption notice to the user message: {Text}", text);
        }
        
        // TODO: Save into some storage
        _chatSessionData.Messages.Add(new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _chatSessionData.UserName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = _chatTextProcessor.ProcessText(text),
        });

        var abortCancellationToken = await _state.BeginGeneratingReply();
        try
        {
            using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, abortCancellationToken);
            var linkedCancellationToken = linkedCancellationSource.Token;

            ChatMessageData reply;
            try
            {
                var gen = await _services.TextGen.GenerateReplyAsync(_chatSessionData, linkedCancellationToken);
                if (string.IsNullOrWhiteSpace(gen.Text)) throw new InvalidOperationException("AI service returned an empty string.");
                reply = CreateMessageFromGen(gen);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            try
            {
                await SendReply(reply, linkedCancellationToken);
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested) return;
#warning This is tricky because we cancel the bot message but we can't say for sure if the response was sent. Requires refactoring.
                _chatSessionData.Messages.Remove(reply);
            }
        }
        finally
        {
            _state.SpeechGenerationComplete();
        }
    }
}