using Voxta.Abstractions;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;

namespace Voxta.Services.FFmpeg;

public sealed class FFmpegSpeechToText : ServiceBase<FFmpegSettings>, ISpeechToTextService
{
    protected override string ServiceName => FFmpegConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    private readonly IRecordingService _recordingService;

    private bool _disposed;

    public event EventHandler? SpeechRecognitionStarted;
    public event EventHandler<string>? SpeechRecognitionPartial;
    public event EventHandler<string?>? SpeechRecognitionFinished;

    public FFmpegSpeechToText(IRecordingService recordingService, ISettingsRepository settingsRepository)
        : base(settingsRepository)
    {
        _recordingService = recordingService;
    }

    protected override async Task<bool> TryInitializeAsync(FFmpegSettings settings, IPrerequisitesValidator prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;

        if (dry) return true;
        
        if (_disposed) throw new ObjectDisposedException(nameof(FFmpegSpeechToText));
        _recordingService.DataAvailable += DataAvailable;
        return true;
    }

    private void DataAvailable(object? sender, RecordingDataEventArgs e)
    {
        if (_disposed) return;

        var accepted = false;
        // If accepted, the transcription is complete. Process e.BytesRecorded here.

        if (accepted)
        {
            _recordingService.Speaking = false;
            SpeechRecognitionFinished?.Invoke(this, "Recognized text");
        }
        else
        {
            if (!_recordingService.Speaking)
            {
                _recordingService.Speaking = true;
                SpeechRecognitionStarted?.Invoke(this, EventArgs.Empty);
            }

            SpeechRecognitionPartial?.Invoke(this, "Partial text");
        }
    }

    public void StartMicrophoneTranscription()
    {
        _recordingService.StartRecording();
    }
    
    public void StopMicrophoneTranscription()
    {
        _recordingService.StopRecording();
    }
    

    public ValueTask DisposeAsync()
    {
        if(_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        _recordingService.StopRecording();
        _recordingService.DataAvailable -= DataAvailable;
        // Dispose any FFmpeg references
        return ValueTask.CompletedTask;
    }
}