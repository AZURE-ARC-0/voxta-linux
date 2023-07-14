using System.Text.Json;
using ChatMate.Abstractions.Services;
using ChatMate.Services.Vosk.Model;
using NAudio.Wave;
using Vosk;

namespace ChatMate.Services.Vosk;

public class VoskSpeechToText : ISpeechToTextService
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    
    private readonly IVoskModelDownloader _modelDownloader;
    private const int SampleRate = 16000;
    
    private VoskRecognizer? _recognizer;
    private WaveInEvent? _waveIn;
    private bool _recording;
    private bool _speaking;
    private bool _disposed;
    
    public event EventHandler? SpeechRecognitionStarted;
    public event EventHandler<string>? SpeechRecognitionFinished;

    public VoskSpeechToText(IVoskModelDownloader modelDownloader)
    {
        _modelDownloader = modelDownloader;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await Semaphore.WaitAsync(cancellationToken);
        var model = await _modelDownloader.AcquireModelAsync(cancellationToken);
        global::Vosk.Vosk.SetLogLevel(-1);
        _recognizer = new VoskRecognizer(model, SampleRate);
        _recognizer.SetWords(true);
        _waveIn = new WaveInEvent();
        _waveIn.WaveFormat = new WaveFormat(SampleRate, 1);
        #warning Add perf measure for this too... somehow
        _waveIn.DataAvailable += (_, e) =>
        {
            if (_disposed) return;
            if (e.BytesRecorded <= 0) return;
            if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                _speaking = false;
                var result = _recognizer.Result();
                var json = JsonSerializer.Deserialize<FinalResult>(result, SerializeOptions);
                if (json?.Result == null || string.IsNullOrEmpty(json.Text)) return;
                if (json.Result is [{ Conf: < 0.9 }]) return;
                SpeechRecognitionFinished?.Invoke(this, json.Text);
            }
            else
            {
                var result = _recognizer.PartialResult();
                var json = JsonSerializer.Deserialize<PartialResult>(result, SerializeOptions);
                if (string.IsNullOrEmpty(json?.Partial)) return;
                if (_speaking) return;
                _speaking = true;
                SpeechRecognitionStarted?.Invoke(this, EventArgs.Empty);
            }
        };
    }
    
    public void StartMicrophoneTranscription()
    {
        if (_recording) return;
        _recording = true;
        _waveIn?.StartRecording();
    }
    
    public void StopMicrophoneTranscription()
    {
        if (!_recording) return;
        _recording = false;
        _waveIn?.StopRecording();
    }
    
    public void Dispose()
    {
        _disposed = true;
        StopMicrophoneTranscription();
        _recognizer?.Dispose();
        _waveIn?.Dispose();
        Semaphore.Release();
    }
}