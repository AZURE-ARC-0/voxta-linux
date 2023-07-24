using System.Security.Authentication;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Voxta.Services.AzureSpeechService;

namespace Microsoft.Extensions.DependencyInjection;

public class AzureSpeechServiceSpeechToText : ISpeechToTextService
{
    private readonly IRecordingService _recordingService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<AzureSpeechServiceSpeechToText> _logger;
    private SpeechRecognizer? _recognizer;
    private PushAudioInputStream? _pushStream;
    private bool _speaking;

    public event EventHandler? SpeechRecognitionStarted;
    public event EventHandler<string>? SpeechRecognitionPartial;
    public event EventHandler<string>? SpeechRecognitionFinished;

    public AzureSpeechServiceSpeechToText(IRecordingService recordingService, ISettingsRepository settingsRepository, ILoggerFactory loggerFactory)
    {
        _recordingService = recordingService;
        _settingsRepository = settingsRepository;
        _logger = loggerFactory.CreateLogger<AzureSpeechServiceSpeechToText>();
    }
    
    public async Task InitializeAsync(string culture, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<AzureSpeechServiceSettings>(cancellationToken);
        if (settings == null) throw new AzureSpeechServiceException("Azure Speech Service is not configured.");
        if (string.IsNullOrEmpty(settings.SubscriptionKey)) throw new AuthenticationException("Azure Speech Service subscription key is missing.");
        if (string.IsNullOrEmpty(settings.Region)) throw new AuthenticationException("Azure Speech Service region is missing.");
        var config = SpeechConfig.FromSubscription(Crypto.DecryptString(settings.SubscriptionKey), settings.Region);
        config.SpeechRecognitionLanguage = culture;
        
        _pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        var audioInput = AudioConfig.FromStreamInput(_pushStream);
        _recognizer = new SpeechRecognizer(config, audioInput);

        _recognizer.Recognizing += (s, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizingSpeech) return;
            _logger.LogDebug("Speech recognizing");
            if (!_speaking)
            {
                _speaking = true;
                SpeechRecognitionStarted?.Invoke(this, EventArgs.Empty);
            }
            SpeechRecognitionPartial?.Invoke(this, e.Result.Text);
        };

        _recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizedSpeech) return;
            _logger.LogDebug("Speech recognized");
            _speaking = false;
            if (!string.IsNullOrEmpty(e.Result.Text))
                SpeechRecognitionFinished?.Invoke(this, e.Result.Text);
        };

        _recognizer.Canceled += (s, e) => {
            _speaking = false;
            _logger.LogDebug("Session canceled event");
        };

        _recognizer.SessionStopped += (_, _) => {
            _logger.LogDebug("Session stopped event");
        };
        
        _recordingService.DataAvailable += (_, e) =>
        {
            _pushStream?.Write(e.Buffer, e.BytesRecorded);
        };
        
        await _recognizer.StartContinuousRecognitionAsync();
    }

    public void StartMicrophoneTranscription()
    {
        _recordingService.StartRecording();
    }
    
    public void StopMicrophoneTranscription()
    {
        _recordingService.StopRecording();
        _speaking = false;
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
