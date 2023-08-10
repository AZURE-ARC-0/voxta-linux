using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;

namespace Voxta.Services.AzureSpeechService.Transcribers;

public sealed class AzureSpeechRecognizer : IAzureSpeechTranscriber
{
    private readonly SpeechRecognizer _recognizer;
    
    public event EventHandler<string>? Partial;
    public event EventHandler<string>? Finished;
    public event EventHandler? Canceled;

    public AzureSpeechRecognizer(SpeechConfig config, AudioConfig audioInput, ILogger logger)
    {
        _recognizer = new SpeechRecognizer(config, audioInput);

        _recognizer.Recognizing += (_, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizingSpeech) return;
            Partial?.Invoke(this, e.Result.Text);
        };

        _recognizer.Recognized += (_, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizedSpeech) return;
            Finished?.Invoke(this, e.Result.Text);
        };

        _recognizer.Canceled += (_, e) => {
            Canceled?.Invoke(this, EventArgs.Empty);
            if (e.Reason == CancellationReason.Error)
                logger.LogError("Error in Azure Speech Service: {ErrorCode} {ErrorDetails}", e.ErrorCode, e.ErrorDetails);
        };

        _recognizer.SessionStopped += (_, _) => {
            logger.LogDebug("Session stopped event");
        };
    }

    public void Dispose()
    {
        _recognizer.StopContinuousRecognitionAsync().Wait();
        _recognizer.Dispose();
    }

    public Task StartContinuousRecognitionAsync()
    {
        return _recognizer.StartContinuousRecognitionAsync();
    }

    public Task StopContinuousRecognitionAsync()
    {
        return _recognizer.StopContinuousRecognitionAsync();
    }
}