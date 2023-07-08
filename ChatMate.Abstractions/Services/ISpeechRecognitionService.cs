namespace ChatMate.Abstractions.Services;

public interface ISpeechRecognitionService : IService
{
    event EventHandler? SpeechStart;
    event EventHandler<string>? SpeechEnd;
    
    void StartMicrophoneTranscription();
    void StopMicrophoneTranscription();
    void Dispose();
}