using ChatMate.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public partial class ChatSession
{
    public void HandleClientMessage(ClientSendMessage clientSendMessage)
    {
        _chatSessionState.AbortGeneratingReplyAsync().AsTask().GetAwaiter().GetResult();
        var abortCancellationToken = _chatSessionState.GenerateReplyBegin();
        Enqueue(ct => HandleClientMessageAsync(clientSendMessage, abortCancellationToken, ct));
    }

    private async ValueTask HandleClientMessageAsync(ClientSendMessage clientSendMessage, CancellationToken abortCancellationToken, CancellationToken queueCancellationToken)
    {
        try
        {
            _logger.LogInformation("Received chat message: {Text}", clientSendMessage.Text);

            var text = clientSendMessage.Text;

            var speechInterruptionRatio = _chatSessionState.InterruptSpeech();
            if (speechInterruptionRatio is > 0.05f and < 0.95f)
            {
                var lastBotMessage = _chatSessionData.Messages.LastOrDefault();
                if (lastBotMessage?.User == _chatSessionData.BotName)
                {
                    var cutoff = Math.Clamp((int)Math.Round(lastBotMessage.Text.Length * speechInterruptionRatio), 1, lastBotMessage.Text.Length - 2);
                    lastBotMessage.Text = lastBotMessage.Text[..cutoff] + "...";
                    lastBotMessage.Tokens = _textGen.GetTokenCount(lastBotMessage.Text);
                    _logger.LogInformation("Cutoff last bot message to account for the interruption: {Text}", lastBotMessage.Text);
                }

                text = "*interrupts {{char}}* " + text;
                _logger.LogInformation("Added interruption notice to the user message: {Text}", text);
            }

            if (_chatSessionState.PendingUserMessage == null)
            {
                var userText = _chatTextProcessor.ProcessText(text);
                var userTextData = new TextData
                {
                    Text = userText,
                    Tokens = _textGen.GetTokenCount(userText)
                };
                var userMessageData = ChatMessageData.FromGen(_chatSessionData.UserName, userTextData);
                _chatSessionData.Messages.Add(userMessageData);
                _chatSessionState.PendingUserMessage = userMessageData;
            }
            else
            {
                var append = '\n' + text;
                _chatSessionState.PendingUserMessage.Text += append;
                _chatSessionState.PendingUserMessage.Tokens += _textGen.GetTokenCount(append);
            }

            ChatMessageData reply;
            try
            {
                using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(queueCancellationToken, abortCancellationToken);
                var linkedCancellationToken = linkedCancellationSource.Token;
                var gen = await _textGen.GenerateReplyAsync(_chatSessionData, linkedCancellationToken);
                if (string.IsNullOrWhiteSpace(gen.Text)) throw new InvalidOperationException("AI service returned an empty string.");
                reply = ChatMessageData.FromGen(_chatSessionData.BotName, gen);
            }
            catch (OperationCanceledException)
            {
                // Reply will simply be dropped
                return;
            }

            _chatSessionState.PendingUserMessage = null;
            
            // TODO: Save into some storage
            _chatSessionData.Messages.Add(reply);
            
            await SendReplyWithSpeechAsync(reply, "speech", queueCancellationToken);
        }
        finally
        {
            _chatSessionState.GenerateReplyEnd();
        }
    }
}
