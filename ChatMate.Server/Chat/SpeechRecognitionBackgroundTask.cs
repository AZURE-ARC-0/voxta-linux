using ChatMate.Abstractions.Services;
using ChatMate.Core;

namespace ChatMate.Server.Chat;

public class SpeechRecognitionBackgroundTask : BackgroundService
{
    private readonly ISpeechRecognitionService _speechRecognitionService;

    public SpeechRecognitionBackgroundTask(ISpeechRecognitionService speechRecognitionService, LocalInputEventDispatcher localInputEventDispatcher)
    {
        _speechRecognitionService = speechRecognitionService;
        speechRecognitionService.SpeechStart += (_, _) => localInputEventDispatcher.OnSpeechRecognitionStart();
        speechRecognitionService.SpeechEnd += (_, text) => localInputEventDispatcher.OnSpeechRecognitionEnd(text);
        localInputEventDispatcher.PauseSpeechRecognition += (_, _) => speechRecognitionService.StopMicrophoneTranscription();
        localInputEventDispatcher.ReadyForSpeechRecognition += (_, _) => speechRecognitionService.StartMicrophoneTranscription();
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