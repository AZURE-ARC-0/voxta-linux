﻿using Voxta.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace Voxta.Core;

public partial class ChatSession
{
    private async ValueTask GenerateReplyAsync(ClientSendMessage clientSendMessage, CancellationToken abortCancellationToken, CancellationToken queueCancellationToken)
    {
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
            _logger.LogInformation("Added interruption notice to the user message. Updated text: {Text}", text);
        }

        if (_chatSessionState.PendingUserMessage == null)
        {
            var userText = _chatTextProcessor.ProcessText(text);
            var userTextData = new TextData
            {
                Text = userText,
                Tokens = _textGen.GetTokenCount(userText)
            };
            var userMessageData = await SaveMessageAsync(_chatSessionData.UserName, userTextData);
            _chatSessionState.PendingUserMessage = userMessageData;
        }
        else
        {
            var append = "; " + text;
            _chatSessionState.PendingUserMessage.Text += append;
            _chatSessionState.PendingUserMessage.Tokens += _textGen.GetTokenCount(append);
            await UpdateMessageAsync(_chatSessionState.PendingUserMessage);
        }

        _chatSessionData.Actions = clientSendMessage.Actions;
        _chatSessionData.Context = _chatTextProcessor.ProcessText(clientSendMessage.Context);

        string generated;
        try
        {
            using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(queueCancellationToken, abortCancellationToken);
            var linkedCancellationToken = linkedCancellationSource.Token;
            generated = await _textGen.GenerateReplyAsync(_chatSessionData, linkedCancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Reply will simply be dropped
            return;
        }
        catch
        {
            if (_pauseSpeechRecognitionDuringPlayback) _speechToText?.StartMicrophoneTranscription();
            throw;
        }

        if (string.IsNullOrWhiteSpace(generated))
        {
            throw new InvalidOperationException("AI service returned an empty string.");
        }

        _chatSessionState.PendingUserMessage = null;
        var genData = new TextData { Text = _sanitizer.Sanitize(generated) };
        var reply = await SaveMessageAsync(_chatSessionData.Character.Name, genData);
        
        _logger.LogInformation("{Character} replied with: {Text}", _chatSessionData.Character.Name, reply.Text);

        var speechId = $"speech_{_chatSessionData.Chat.Id}_{reply.Id}";
        await SendReplyWithSpeechAsync(reply.Text, speechId, false, queueCancellationToken);
        await GenerationActionInference(queueCancellationToken);
    }
}
