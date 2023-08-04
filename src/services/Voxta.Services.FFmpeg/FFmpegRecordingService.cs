using Voxta.Abstractions.Services;

namespace Voxta.Services.FFmpeg;

public class FFmpegRecordingService : IRecordingService
{
    private bool _recording;
    private bool _disposed;

    public bool Speaking { get; set; }
    public event EventHandler<RecordingDataEventArgs>? DataAvailable;

    public FFmpegRecordingService()
    {
        // When recording, invoke event: DataAvailable?.Invoke(s, new RecordingDataEventArgs(e.Buffer, e.BytesRecorded)):
        DataAvailable?.Invoke(this, new RecordingDataEventArgs(Array.Empty<byte>(), 0));
    }

    public void StartRecording()
    {
        if (_disposed) return;
        if (_recording) return;
        _recording = true;
        // Start recording
    }

    public void StopRecording()
    {
        Speaking = false;
        if (!_recording) return;
        _recording = false;
        // Stop recording
    }

    public void Dispose()
    {
        _disposed = true;
        StopRecording();
        // Dispose any FFmpeg references
    }
}