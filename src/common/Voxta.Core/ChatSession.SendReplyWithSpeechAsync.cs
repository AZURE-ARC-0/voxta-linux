using Voxta.Abstractions.Model;
using Voxta.Common;

namespace Voxta.Core;

public partial class ChatSession
{
    private Task SendReusableReplyWithSpeechAsync(string text, CancellationToken cancellationToken)
    {
        var speechId = Crypto.CreateSha1Hash($"{_chatSessionData.Character.Services.SpeechGen.Voice ?? "NULL"}::{text}");
        return SendReplyWithSpeechAsync(text, speechId, true, cancellationToken);
    }

    private async Task SendReplyWithSpeechAsync(string text, string speechId, bool reusable, CancellationToken cancellationToken)
    {
        var speechTask = Task.Run(() => _speechGenerator.CreateSpeechAsync(text, speechId, reusable, cancellationToken), cancellationToken);

        await _tunnel.SendAsync(new ServerReplyMessage
        {
            Text = text,
        }, cancellationToken);

        var speechUrl = await speechTask;
        if (speechUrl != null)
        {
            if (_pauseSpeechRecognitionDuringPlayback) _speechToText?.StopMicrophoneTranscription();
            await _tunnel.SendAsync(new ServerSpeechMessage
            {
                Url = speechUrl,
            }, cancellationToken);
        }
    }
}