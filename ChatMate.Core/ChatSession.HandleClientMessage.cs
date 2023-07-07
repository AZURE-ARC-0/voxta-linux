using ChatMate.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public partial class ChatSession
{
    public void HandleClientMessage(ClientSendMessage clientSendMessage)
    {
        Enqueue(ct => HandleClientMessageAsync(clientSendMessage, ct));
    }

    private async ValueTask HandleClientMessageAsync(ClientSendMessage clientSendMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received chat message: {Text}", clientSendMessage.Text);
#warning This should actually happen once we have the text and sent the wav back
        if (_pauseSpeechRecognitionDuringPlayback) _inputHandle?.RequestPauseSpeechRecognition();

        var text = clientSendMessage.Text;

        if (await _chatSessionState.AbortReplyAsync())
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

        var abortCancellationToken = await _chatSessionState.BeginGeneratingReply();
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
            _chatSessionState.SpeechGenerationComplete();
        }
    }
}