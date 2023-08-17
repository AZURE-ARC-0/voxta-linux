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
            switch (_chatSessionState.State)
            {
                case ChatSessionStates.Live:
                    if (clientSendMessage.Text.Contains("go offline", StringComparison.InvariantCultureIgnoreCase))
                        await EnterPauseMode(queueCancellationToken);
                    else if (clientSendMessage.Text.Contains("analysis mode", StringComparison.InvariantCultureIgnoreCase))
                        await EnterAnalysisMode(queueCancellationToken);
                    else
                        await GenerateReplyAsync(clientSendMessage, abortCancellationToken, queueCancellationToken);
                    break;
                case ChatSessionStates.Paused:
                    if (clientSendMessage.Text.Contains("go online", StringComparison.InvariantCultureIgnoreCase))
                        await EnterLiveMode(queueCancellationToken);
                    else if (clientSendMessage.Text.Contains("analysis mode", StringComparison.InvariantCultureIgnoreCase))
                        await EnterAnalysisMode(queueCancellationToken);
                    else
                        // TODO: Workaround because the client does not know we are offline and waits for the reply
                        await SendReusableReplyWithSpeechAsync(".", queueCancellationToken);
                    break;
                case ChatSessionStates.Analysis:
                    if (clientSendMessage.Text.Contains("go online", StringComparison.InvariantCultureIgnoreCase))
                        await EnterLiveMode(queueCancellationToken);
                    else if (clientSendMessage.Text.Contains("go offline", StringComparison.InvariantCultureIgnoreCase))
                        await EnterPauseMode(queueCancellationToken);
                    else
                        await HandleAnalysisModeAsync(clientSendMessage, queueCancellationToken);
                    break;
                default:
                    throw new NotSupportedException($"Unknown state: {_chatSessionState.State}");
            }
        }
        finally
        {
            _chatSessionState.GenerateReplyEnd();
        }
    }

    private async Task EnterAnalysisMode(CancellationToken queueCancellationToken)
    {
        _chatSessionState.State = ChatSessionStates.Analysis;
        _speechToText?.StartMicrophoneTranscription();
        await SendReplyWithSpeechAsync("Entering analysis mode.", $"analysis_{Guid.NewGuid()}", false, queueCancellationToken);
    }

    private async Task EnterLiveMode(CancellationToken queueCancellationToken)
    {
        _chatSessionState.State = ChatSessionStates.Live;
        _speechToText?.StartMicrophoneTranscription();
        await SendReusableReplyWithSpeechAsync("Back online.", queueCancellationToken);
    }

    private async Task EnterPauseMode(CancellationToken queueCancellationToken)
    {
        _chatSessionState.State = ChatSessionStates.Paused;
        _speechToText?.StopMicrophoneTranscription();
        await SendReusableReplyWithSpeechAsync("Going offline.", queueCancellationToken);
    }
}
