using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Extensions.Logging;

namespace Voxta.Services.AzureSpeechService.Transcribers;

public sealed class AzureSpeechConversationTranscriber : IAzureSpeechTranscriber
{
    private readonly ConversationTranscriber _transcriber;
    private string? _speakerId;
    
    public event EventHandler<string>? Partial;
    public event EventHandler<string>? Finished;
    public event EventHandler? Canceled;

    public AzureSpeechConversationTranscriber(SpeechConfig config, AudioConfig audioInput, ILogger logger)
    {
        _transcriber = new ConversationTranscriber(config, audioInput);

        _transcriber.Transcribing  += (_, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizingSpeech) return;
            Partial?.Invoke(this, e.Result.Text);
        };

        _transcriber.Transcribed  += (_, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizedSpeech) return;
            if (_speakerId == null && !string.IsNullOrEmpty(e.Result.SpeakerId) && e.Result.SpeakerId != "Unknown") _speakerId = e.Result.SpeakerId;
            else if (_speakerId != null && !string.IsNullOrEmpty(e.Result.SpeakerId) && _speakerId != e.Result.SpeakerId && e.Result.SpeakerId != "Unknown") return;
            Finished?.Invoke(this, e.Result.Text);
        };

        _transcriber.Canceled += (_, e) => {
            Canceled?.Invoke(this, EventArgs.Empty);
            if (e.Reason == CancellationReason.Error)
                logger.LogError("Error in Azure Speech Service: {ErrorCode} {ErrorDetails}", e.ErrorCode, e.ErrorDetails);
        };

        _transcriber.SessionStopped += (_, _) => {
            logger.LogDebug("Session stopped event");
        };
    }

    public void Dispose()
    {
        _transcriber.StopTranscribingAsync().Wait();
        _transcriber.Dispose();
    }

    public Task StartContinuousRecognitionAsync()
    {
        return _transcriber.StartTranscribingAsync();
    }

    public Task StopContinuousRecognitionAsync()
    {
        return _transcriber.StopTranscribingAsync();
    }
}