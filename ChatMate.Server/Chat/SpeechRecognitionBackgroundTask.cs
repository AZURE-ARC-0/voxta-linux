using ChatMate.Abstractions.Services;
using ChatMate.Core;

namespace ChatMate.Server.Chat;

public class SpeechRecognitionBackgroundTask : BackgroundService
{
    private readonly ISpeechRecognitionService _speechRecognitionService;

    public SpeechRecognitionBackgroundTask(ISpeechRecognitionService speechRecognitionService, ExclusiveLocalInputManager exclusiveLocalInputManager)
    {
        _speechRecognitionService = speechRecognitionService;
        speechRecognitionService.SpeechStart += (_, _) => exclusiveLocalInputManager.OnSpeechRecognitionStarted();
        speechRecognitionService.SpeechEnd += (_, text) => exclusiveLocalInputManager.OnSpeechRecognitionFinished(text);
        exclusiveLocalInputManager.PauseSpeechRecognitionRequested += (_, _) => speechRecognitionService.StopMicrophoneTranscription();
        exclusiveLocalInputManager.ResumeSpeechRecognitionRequested += (_, _) => speechRecognitionService.StartMicrophoneTranscription();
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _speechRecognitionService.InitializeAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _speechRecognitionService.StopMicrophoneTranscription();
        _speechRecognitionService.Dispose();
        base.Dispose();
    }
}