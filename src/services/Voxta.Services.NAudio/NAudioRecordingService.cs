using Voxta.Abstractions.Services;
using NAudio.Wave;

namespace Voxta.Services.NAudio;

public class NAudioRecordingService : IRecordingService
{
    private const int SampleRate = 16000;
    
    private readonly WaveInEvent _waveIn;
    private bool _recording;
    private bool _disposed;

    public event EventHandler<RecordingDataEventArgs>? DataAvailable;

    public NAudioRecordingService()
    {
        _waveIn = new WaveInEvent();
        _waveIn.WaveFormat = new WaveFormat(SampleRate, 1);
        _waveIn.DataAvailable += (s, e) =>
        {
            if (_disposed) return;
            if (e.BytesRecorded <= 0) return;
            DataAvailable?.Invoke(s, new RecordingDataEventArgs(e.Buffer, e.BytesRecorded));
        };
    }

    public void StartRecording()
    {
        if (_disposed) return;
        if (_recording) return;
        _recording = true;
        _waveIn.StartRecording();
    }

    public void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        _waveIn.StopRecording();
    }

    public void Dispose()
    {
        _disposed = true;
        StopRecording();
        _waveIn.Dispose();
    }
}