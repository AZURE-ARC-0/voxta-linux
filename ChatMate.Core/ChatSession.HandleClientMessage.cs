using ChatMate.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public partial class ChatSession
{
    public void HandleClientMessage(ClientSendMessage clientSendMessage)
    {
        _chatSessionState.AbortGeneratingReplyAsync().AsTask().GetAwaiter().GetResult();
        var abortCancellationToken = _chatSessionState.GenerateReplyBegin(_performanceMetrics.Start("GenerateReply.Total"));
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
                var lastCharacterMessage = _chatSessionData.Messages.LastOrDefault();
                if (lastCharacterMessage?.User == _chatSessionData.Character.Name)
                {
                    var cutoff = Math.Clamp((int)Math.Round(lastCharacterMessage.Text.Length * speechInterruptionRatio), 1, lastCharacterMessage.Text.Length - 2);
                    lastCharacterMessage.Text = lastCharacterMessage.Text[..cutoff] + "...";
                    lastCharacterMessage.Tokens = _textGen.GetTokenCount(lastCharacterMessage.Text);
                    _logger.LogInformation("Cutoff last character message to account for the interruption: {Text}", lastCharacterMessage.Text);
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

            _chatSessionData.Actions = clientSendMessage.Actions;
            _chatSessionData.Context = _chatTextProcessor.ProcessText(clientSendMessage.Context);

            ChatMessageData reply;
            try
            {
                using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(queueCancellationToken, abortCancellationToken);
                var linkedCancellationToken = linkedCancellationSource.Token;
                var gen = await _textGen.GenerateReplyAsync(_chatSessionData, linkedCancellationToken);
                if (string.IsNullOrWhiteSpace(gen.Text))
                {
                    throw new InvalidOperationException("AI service returned an empty string.");
                }

                reply = ChatMessageData.FromGen(_chatSessionData.Character.Name, gen);
            }
            catch (OperationCanceledException)
            {
                // Reply will simply be dropped
                return;
            }
            catch
            {
                if (_pauseSpeechRecognitionDuringPlayback) _inputHandle?.RequestResumeSpeechRecognition();
                throw;
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
