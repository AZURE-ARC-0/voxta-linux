using System.Text.Json;
using ChatMate.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using Vosk;

namespace ChatMate.Services.Vosk;

public class VoskSpeechRecognition : ISpeechRecognitionService
{
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    
    private readonly IVoskModelDownloader _modelDownloader;
    private readonly IOptions<VoskOptions> _options;
    private const int SampleRate = 16000;
    
    private VoskRecognizer? _recognizer;
    private WaveInEvent? _waveIn;
    private bool _recording;
    private bool _speaking;
    
    public event EventHandler? SpeechStart;
    public event EventHandler<string>? SpeechEnd;

    public VoskSpeechRecognition(IVoskModelDownloader modelDownloader, IOptions<VoskOptions> options)
    {
        _modelDownloader = modelDownloader;
        _options = options;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var model = await _modelDownloader.AcquireModelAsync();
        global::Vosk.Vosk.SetLogLevel(_options.Value.LogLevel);
        _recognizer = new VoskRecognizer(model, SampleRate);
        _recognizer.SetWords(true);
        _waveIn = new WaveInEvent();
        _waveIn.WaveFormat = new WaveFormat(SampleRate, 1);
        #warning Add perf measure for this too... somehow
        _waveIn.DataAvailable += (_, e) =>
        {
            if (e.BytesRecorded <= 0) return;
            if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                _speaking = false;
                var result = _recognizer.Result();
                var json = JsonSerializer.Deserialize<Result>(result, SerializeOptions);
                var text = json?.Text;
                if (string.IsNullOrEmpty(text)) return;
                if (text == "huh") return;
                SpeechEnd?.Invoke(this, text);
            }
            else
            {
                var result = _recognizer.PartialResult();
                var json = JsonSerializer.Deserialize<PartialResult>(result, SerializeOptions);
                if (string.IsNullOrEmpty(json?.Partial)) return;
                if (_speaking) return;
                _speaking = true;
                SpeechStart?.Invoke(this, EventArgs.Empty);
            }
        };
    }
    
    public void StartMicrophoneTranscription()
    {
        if (_recording) return;
        _waveIn?.StartRecording();
        _recording = true;
    }
    
    public void StopMicrophoneTranscription()
    {
        if (!_recording) return;
        _recording = false;
        _waveIn?.StopRecording();
    }
    
    public void Dispose()
    {
        StopMicrophoneTranscription();
        _waveIn?.Dispose();
        _recognizer?.Dispose();
    }
}