using Voxta.Abstractions.Services;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;

namespace Voxta.Services.FFmpeg;

public sealed class FFmpegSpeechToText : ISpeechToTextService
{
    public string ServiceName => FFmpegConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    private readonly IRecordingService _recordingService;
    private readonly ISettingsRepository _settingsRepository;

    private bool _disposed;
    private bool _initialized;

    public event EventHandler? SpeechRecognitionStarted;
    public event EventHandler<string>? SpeechRecognitionPartial;
    public event EventHandler<string>? SpeechRecognitionFinished;

    public FFmpegSpeechToText(IRecordingService recordingService, ISettingsRepository settingsRepository)
    {
        _recordingService = recordingService;
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> TryInitializeAsync(Guid serviceId, string[] prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        if (_initialized) return true;
        _initialized = true;
        var settings = await _settingsRepository.GetAsync<FFmpegSettings>(TODO, cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
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
    
    public void Dispose()
    {
        if(_disposed) return;
        _disposed = true;
        _recordingService.StopRecording();
        _recordingService.DataAvailable -= DataAvailable;
        // Dispose any FFmpeg references
    }
}