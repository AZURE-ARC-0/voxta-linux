using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Voxta.Services.AzureSpeechService;

namespace Microsoft.Extensions.DependencyInjection;

public class AzureSpeechServiceSpeechToText : ISpeechToTextService
{
    public string ServiceName => AzureSpeechServiceConstants.ServiceName;
    public string[] Features { get; private set; } = Array.Empty<string>();
    
    private readonly IRecordingService _recordingService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<AzureSpeechServiceSpeechToText> _logger;
    private SpeechRecognizer? _recognizer;
    private PushAudioInputStream? _pushStream;

    public event EventHandler? SpeechRecognitionStarted;
    public event EventHandler<string>? SpeechRecognitionPartial;
    public event EventHandler<string>? SpeechRecognitionFinished;

    public AzureSpeechServiceSpeechToText(IRecordingService recordingService, ISettingsRepository settingsRepository, ILoggerFactory loggerFactory)
    {
        _recordingService = recordingService;
        _settingsRepository = settingsRepository;
        _logger = loggerFactory.CreateLogger<AzureSpeechServiceSpeechToText>();
    }
    
    public async Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<AzureSpeechServiceSettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        if (string.IsNullOrEmpty(settings.SubscriptionKey)) return false;
        if (string.IsNullOrEmpty(settings.Region)) return false;
        if (prerequisites.Contains(ServiceFeatures.NSFW) && settings.FilterProfanity) return false;
        
        var config = SpeechConfig.FromSubscription(Crypto.DecryptString(settings.SubscriptionKey), settings.Region);
        config.SpeechRecognitionLanguage = culture;
        if (settings.FilterProfanity)
        {
            config.SetProfanity(ProfanityOption.Removed);
        }
        else
        {
            config.SetProfanity(ProfanityOption.Raw);
            Features = new[] { ServiceFeatures.NSFW };
        }
        
        if (!string.IsNullOrEmpty(settings.LogFilename))
        {
            var directory = Path.GetDirectoryName(settings.LogFilename) ?? throw new AzureSpeechServiceException($"Invalid log filename: {settings.LogFilename}");
            Directory.CreateDirectory(directory);
            var filename = DateTimeOffset.Now.ToString("yy-MM-dd") + "_stt_" + Path.GetFileName(settings.LogFilename);
            config.SetProperty(PropertyId.Speech_LogFilename, Path.Combine(directory, filename));
        }
        
        _pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        var audioInput = AudioConfig.FromStreamInput(_pushStream);
        _recognizer = new SpeechRecognizer(config, audioInput);

        _recognizer.Recognizing += (_, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizingSpeech) return;
            _logger.LogDebug("Speech recognizing");
            if (!_recordingService.Speaking)
            {
                _recordingService.Speaking = true;
                SpeechRecognitionStarted?.Invoke(this, EventArgs.Empty);
            }
            SpeechRecognitionPartial?.Invoke(this, e.Result.Text);
        };

        _recognizer.Recognized += (_, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizedSpeech) return;
            _logger.LogDebug("Speech recognized");
            _recordingService.Speaking = false;
            if (!string.IsNullOrEmpty(e.Result.Text))
                SpeechRecognitionFinished?.Invoke(this, e.Result.Text);
        };

        _recognizer.Canceled += (_, e) => {
            _recordingService.Speaking = false;
            if (e.Reason == CancellationReason.Error)
                _logger.LogError("Error in Azure Speech Service: {ErrorCode} {ErrorDetails}", e.ErrorCode, e.ErrorDetails);
            else
                _logger.LogDebug("Session canceled: {Reason}", e.Reason);
        };

        _recognizer.SessionStopped += (_, _) => {
            _logger.LogDebug("Session stopped event");
        };
        
        _recordingService.DataAvailable += (_, e) =>
        {
            _pushStream?.Write(e.Buffer, e.BytesRecorded);
        };
        
        await _recognizer.StartContinuousRecognitionAsync();
        return true;
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
        _recordingService.StopRecording();
        _recognizer?.StopContinuousRecognitionAsync().Wait();
        _recognizer?.Dispose();
        _recognizer = null;
        _pushStream?.Close();
        _pushStream?.Dispose();
        _pushStream = null;
    }
}
