using System.Text.Json;
using Voxta.Abstractions.Services;
using Voxta.Services.Vosk.Model;
using Vosk;
using Voxta.Abstractions;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;

namespace Voxta.Services.Vosk;

public sealed class VoskSpeechToText : ServiceBase<VoskSettings>, ISpeechToTextService
{
    private const int SampleRate = 16000;
    
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    protected override string ServiceName => VoskConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    private readonly IVoskModelDownloader _modelDownloader;
    private readonly IRecordingService _recordingService;

    private VoskRecognizer? _recognizer;
    private bool _disposed;
    private bool _semaphoreInitialized;
    private string[]? _ignoredWords;

    public event EventHandler? SpeechRecognitionStarted;
    public event EventHandler<string>? SpeechRecognitionPartial;
    public event EventHandler<string?>? SpeechRecognitionFinished;

    public VoskSpeechToText(IVoskModelDownloader modelDownloader, IRecordingService recordingService, ISettingsRepository settingsRepository)
        : base(settingsRepository)
    {
        _modelDownloader = modelDownloader;
        _recordingService = recordingService;
    }

    static VoskSpeechToText()
    {
        global::Vosk.Vosk.SetLogLevel(-1);
    }

    protected override async Task<bool> TryInitializeAsync(VoskSettings settings, IPrerequisitesValidator prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;
        
        if (string.IsNullOrEmpty(settings.Model)) return false;
        if (!settings.Model.Contains(culture, StringComparison.InvariantCultureIgnoreCase)) return false;
        if (dry) return true;
        
        if (_disposed) throw new ObjectDisposedException(nameof(VoskSpeechToText));
        await Semaphore.WaitAsync(cancellationToken);
        _semaphoreInitialized = true;
        if (_disposed) return false;
        var model = await _modelDownloader.AcquireModelAsync(settings.Model, settings.ModelHash, cancellationToken);
        _ignoredWords = settings.IgnoredWords;
        _recognizer = new VoskRecognizer(model, SampleRate);
        _recognizer.SetWords(true);
        _recordingService.DataAvailable += DataAvailable;
        return true;
    }

    private void DataAvailable(object? sender, RecordingDataEventArgs e)
    {
        if (_disposed || _recognizer == null) return;
        bool accepted;
        try
        {
            accepted = _recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded);
        }
        catch (AccessViolationException)
        {
            return;
        }

        if (accepted)
        {
            _recordingService.Speaking = false;
            var result = _recognizer.Result();
            var json = JsonSerializer.Deserialize<FinalResult>(result, SerializeOptions);
            string? text;
            if (json == null || string.IsNullOrEmpty(json.Text)) text = null;
            else switch (json.Result)
            {
                case null:
                case [{ Conf: < 0.99 }]:
                    text = null;
                    break;
                default:
                {
                    text = json.Result.Length switch
                    {
                        1 when IsNoise(json.Text) => null,
                        _ => json.Text
                    };
                    break;
                }
            }
            SpeechRecognitionFinished?.Invoke(this, text);
        }
        else
        {
            var result = _recognizer.PartialResult();
            var json = JsonSerializer.Deserialize<PartialResult>(result, SerializeOptions);
            if (json == null || string.IsNullOrEmpty(json.Partial)) return;
            if (IsNoise(json.Partial)) return;
            if (!_recordingService.Speaking)
            {
                _recordingService.Speaking = true;
                SpeechRecognitionStarted?.Invoke(this, EventArgs.Empty);
            }

            SpeechRecognitionPartial?.Invoke(this, json.Partial);
        }
    }

    private bool IsNoise(string word)
    {
        return _ignoredWords?.Contains(word) ?? false;
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
        _recognizer?.Dispose();
        try
        {
            if (_semaphoreInitialized) Semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
        }
        return ValueTask.CompletedTask;
    }
}