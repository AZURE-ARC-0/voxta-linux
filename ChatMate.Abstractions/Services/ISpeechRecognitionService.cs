namespace ChatMate.Abstractions.Services;

public interface ISpeechRecognitionService
{
    event EventHandler? SpeechStart;
    event EventHandler<string>? SpeechEnd;
    
    Task InitializeAsync();
    void StartMicrophoneTranscription();
    void StopMicrophoneTranscription();
    void Dispose();
}