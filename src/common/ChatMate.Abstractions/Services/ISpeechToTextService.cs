namespace ChatMate.Abstractions.Services;

public interface ISpeechToTextService : IService
{
    event EventHandler? SpeechRecognitionStarted;
    event EventHandler<string>? SpeechRecognitionFinished;
    
    void StartMicrophoneTranscription();
    void StopMicrophoneTranscription();
    
    void Dispose();
}