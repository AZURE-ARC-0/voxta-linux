namespace ChatMate.Abstractions.Services;

public interface ISpeechToTextService : IService, IDisposable
{
    event EventHandler? SpeechRecognitionStarted;
    event EventHandler<string>? SpeechRecognitionFinished;
    
    void StartMicrophoneTranscription();
    void StopMicrophoneTranscription();
}