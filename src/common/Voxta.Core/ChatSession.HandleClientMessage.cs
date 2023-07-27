using System.Text;
using Voxta.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace Voxta.Core;

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
        _logger.LogInformation("Received chat message: {Text}", clientSendMessage.Text);

        try
        {
            var lower = clientSendMessage.Text.ToLower();
            switch (_chatSessionState.State)
            {
                case ChatSessionStates.Live:
                    if (lower.Contains("go offline"))
                        await EnterOfflineMode(queueCancellationToken);
                    else if (lower.Contains("analysis mode"))
                        await EnterDiagnosticsMode(queueCancellationToken);
                    else
                        await GenerateReplyAsync(clientSendMessage, abortCancellationToken, queueCancellationToken);
                    break;
                case ChatSessionStates.Paused:
                    if (lower.Contains("go online"))
                        await EnterOnlineMode(queueCancellationToken);
                    else if (lower.Contains("analysis mode"))
                        await EnterDiagnosticsMode(queueCancellationToken);
                    else
                        // TODO: Workaround because the client does not know we are offline and waits for the reply
                        await SendReusableReplyWithSpeechAsync(".", queueCancellationToken);
                    break;
                case ChatSessionStates.Diagnostics:
                    if (lower.Contains("go online"))
                        await EnterOnlineMode(queueCancellationToken);
                    else if (lower.Contains("go offline"))
                        await EnterOfflineMode(queueCancellationToken);
                    else if (lower.StartsWith("repeat") && lower.Length > 7)
                        await SendReplyWithSpeechAsync(clientSendMessage.Text[7..], $"diag_{Guid.NewGuid()}", false, queueCancellationToken);
                    else
                        await SendReusableReplyWithSpeechAsync("Unknown command.", queueCancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        finally
        {
            _chatSessionState.GenerateReplyEnd();
        }
    }

    private async Task EnterDiagnosticsMode(CancellationToken queueCancellationToken)
    {
        _chatSessionState.State = ChatSessionStates.Diagnostics;
        var sb = new StringBuilder();
        sb.AppendLineLinux("Diagnostics for character " + _chatSessionData.Character.Name);
        sb.AppendLineLinux("Tex Generation: " + _textGen.ServiceName);
        sb.AppendLineLinux("Text To Speech: " + _speechGenerator.ServiceName + " with voice " + _speechGenerator.Voice);
        sb.AppendLineLinux("Action Inference: " + (_actionInference?.ServiceName ?? "None"));
        sb.AppendLineLinux("Speech To Text: " + (_speechToText?.ServiceName ?? "None"));
        await SendReplyWithSpeechAsync(sb.ToString(), $"diagnostics_{Guid.NewGuid()}", false, queueCancellationToken);
    }

    private async Task EnterOnlineMode(CancellationToken queueCancellationToken)
    {
        _chatSessionState.State = ChatSessionStates.Live;
        await SendReusableReplyWithSpeechAsync("I'm now online!", queueCancellationToken);
    }

    private async Task EnterOfflineMode(CancellationToken queueCancellationToken)
    {
        _chatSessionState.State = ChatSessionStates.Paused;
        await SendReusableReplyWithSpeechAsync("Going offline.", queueCancellationToken);
    }
}
