using Voxta.Abstractions.Services;
using NAudio.Wave;

namespace Voxta.Services.NAudio;

public class NAudioRecordingService : IRecordingService
{
    private const int _sampleRate = 16000;
    #if(SKIP_EMPTY_AUDIO)
    private const double _silenceThreshold = 1200;
    #endif
    
    private readonly WaveInEvent _waveIn;
    private bool _recording;
    private bool _disposed;

    public bool Speaking { get; set; }
    public event EventHandler<RecordingDataEventArgs>? DataAvailable;

    public NAudioRecordingService()
    {
        _waveIn = new WaveInEvent();
        _waveIn.WaveFormat = new WaveFormat(_sampleRate, 1);
        _waveIn.DataAvailable += (s, e) =>
        {
            if (_disposed) return;
            if (e.BytesRecorded <= 0) return;
            #if(SKIP_EMPTY_AUDIO)
            if (!Speaking)
            {
                var rms = CalculateRms(e.Buffer, e.BytesRecorded);
                if (rms < _silenceThreshold)
                {
                    return;
                }
            }
            #endif
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
        Speaking = false;
        if (!_recording) return;
        _recording = false;
        _waveIn.StopRecording();
    }

    // ReSharper disable once UnusedMember.Local
    private static double CalculateRms(byte[] buffer, int bytesRecorded)
    {
        double rms = 0;
        var samples = bytesRecorded / 2; // 16 bit audio
        for (var i = 0; i < samples; i++)
        {
            var sample = BitConverter.ToInt16(buffer, i * 2);
            rms += sample * sample;
        }
        rms /= samples;
        rms = Math.Sqrt(rms);
        return rms;
    }

    public void Dispose()
    {
        _disposed = true;
        StopRecording();
        _waveIn.Dispose();
    }
}