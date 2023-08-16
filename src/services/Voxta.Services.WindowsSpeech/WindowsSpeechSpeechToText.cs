#if(WINDOWS)
using System.Globalization;
using System.Speech.Recognition;
using Microsoft.Extensions.Logging;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Services.WindowsSpeech;

public class WindowsSpeechSpeechToText : ISpeechToTextService
{
    public string ServiceName => WindowsSpeechConstants.ServiceName;
    public string[] Features { get; } = Array.Empty<string>();
    
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<WindowsSpeechSpeechToText> _logger;
    private SpeechRecognitionEngine ? _recognizer;
    private bool _speaking;
    private bool _activated;
    private bool _stopping;
    private bool _disposed;

    public event EventHandler? SpeechRecognitionStarted;
    public event EventHandler<string>? SpeechRecognitionPartial;
    public event EventHandler<string>? SpeechRecognitionFinished;

    public WindowsSpeechSpeechToText(ISettingsRepository settingsRepository, ILoggerFactory loggerFactory)
    {
        _settingsRepository = settingsRepository;
        _logger = loggerFactory.CreateLogger<WindowsSpeechSpeechToText>();
    }
    
    public async Task<bool> TryInitializeAsync(Guid serviceId, string[] prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<WindowsSpeechSettings>(TODO, cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        if (prerequisites.Contains(ServiceFeatures.NSFW)) return false;
        if (dry) return true;

        _recognizer = new SpeechRecognitionEngine(CultureInfo.GetCultureInfoByIetfLanguageTag(culture));
        var grammar = new DictationGrammar();
        _recognizer.LoadGrammar(grammar);
        _recognizer.SetInputToDefaultAudioDevice();
        
        _recognizer.SpeechDetected += (_, _) =>
        {
            _logger.LogDebug("Speech detected");
            if (_speaking) return;
            _speaking = true;
            SpeechRecognitionStarted?.Invoke(this, EventArgs.Empty);
        };
        
        _recognizer.SpeechHypothesized += (_, e) =>
        {
            _logger.LogDebug("Speech recognizing");
            SpeechRecognitionPartial?.Invoke(this, e.Result.Text);
        };

        _recognizer.SpeechRecognized += (_, e) =>
        {
            _logger.LogDebug("Speech recognized");
            _speaking = false;
            if (!string.IsNullOrEmpty(e.Result.Text) && e.Result.Confidence > settings.MinimumConfidence)
                SpeechRecognitionFinished?.Invoke(this, e.Result.Text);
        };

        _recognizer.SpeechRecognitionRejected += (_, _) => {
            _speaking = false;
        };

        _recognizer.RecognizeCompleted += (_, _) =>
        {
            _stopping = false;
        };

        _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        _activated = true;
        return true;
    }

    public void StartMicrophoneTranscription()
    {
        if (_activated) return;
        if (_stopping) throw new InvalidOperationException("Cannot restart, recognizer wasn't stopped yet.");
        _recognizer?.RecognizeAsync(RecognizeMode.Multiple);
        _activated = true;
    }
    
    public void StopMicrophoneTranscription()
    {
        if(!_activated) return;
        _recognizer?.RecognizeAsyncCancel();
        _activated = false;
    }
    
    public void Dispose()
    {
        if(_disposed) return;
        _disposed = true;
        if (_activated)
        {
            try
            {
                _recognizer?.RecognizeAsyncCancel();
            }
            catch
            {
                // ignored
            }
            _activated = false;
        }
        _recognizer?.Dispose();
        _recognizer = null;
    }
}
#endif
