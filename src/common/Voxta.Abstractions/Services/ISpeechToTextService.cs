namespace Voxta.Abstractions.Services;

public interface ISpeechToTextService : IService
{
    event EventHandler? SpeechRecognitionStarted;
    
    event EventHandler<string>? SpeechRecognitionPartial;
    event EventHandler<string?>? SpeechRecognitionFinished;
    
    void StartMicrophoneTranscription();
    void StopMicrophoneTranscription();
}