using System.Text.Json;
using ChatMate.Abstractions.Services;
using ChatMate.Services.Vosk.Model;
using Vosk;

namespace ChatMate.Services.Vosk;

public sealed class VoskSpeechToText : ISpeechToTextService
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    
    private readonly IVoskModelDownloader _modelDownloader;
    private readonly IRecordingService _recordingService;
    private const int SampleRate = 16000;
    
    private VoskRecognizer? _recognizer;
    private bool _disposed;
    private bool _initialized;
    
    public event EventHandler? SpeechRecognitionStarted;
    public event EventHandler<string>? SpeechRecognitionFinished;

    public VoskSpeechToText(IVoskModelDownloader modelDownloader, IRecordingService recordingService)
    {
        _modelDownloader = modelDownloader;
        _recordingService = recordingService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VoskSpeechToText));
        if (_initialized) return;
        _initialized = true;
        await Semaphore.WaitAsync(cancellationToken);
        if (_disposed) return;
        var model = await _modelDownloader.AcquireModelAsync(cancellationToken);
        global::Vosk.Vosk.SetLogLevel(-1);
        _recognizer = new VoskRecognizer(model, SampleRate);
        _recognizer.SetWords(true);
        _recordingService.DataAvailable += (_, e) =>
        {
            if (_disposed) return;
            if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                var result = _recognizer.Result();
                var json = JsonSerializer.Deserialize<FinalResult>(result, SerializeOptions);
                if (json?.Result == null || string.IsNullOrEmpty(json.Text)) return;
                if (json.Result is [{ Conf: < 0.99 }]) return;
                #warning This might change for different languages
                if (json.Result.Length == 1 && json.Text is "the" or "huh") return;
                SpeechRecognitionFinished?.Invoke(this, json.Text);
            }
            #if(VOSK_PARTIAL)
            else
            {
                var result = _recognizer.PartialResult();
                var json = JsonSerializer.Deserialize<PartialResult>(result, SerializeOptions);
                if (string.IsNullOrEmpty(json?.Partial)) return;
                if (_speaking) return;
                _speaking = true;
                SpeechRecognitionStarted?.Invoke(this, EventArgs.Empty);
            }
            #endif
        };
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
        _recognizer?.Dispose();
        try
        {
            if (_initialized) Semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }
}