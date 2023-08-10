namespace Voxta.Services.AzureSpeechService.Transcribers;

public interface IAzureSpeechTranscriber : IDisposable
{
    event EventHandler<string>? Partial;
    event EventHandler<string>? Finished;
    event EventHandler? Canceled;
    Task StartContinuousRecognitionAsync();
    Task StopContinuousRecognitionAsync();
}