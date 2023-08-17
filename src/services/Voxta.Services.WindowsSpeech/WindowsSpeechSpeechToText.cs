#if(WINDOWS)
using System.Globalization;
using System.Speech.Recognition;
using Microsoft.Extensions.Logging;
using Voxta.Abstractions;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Services.WindowsSpeech;

public class WindowsSpeechSpeechToText : ServiceBase<WindowsSpeechSettings>, ISpeechToTextService
{
    protected override string ServiceName => WindowsSpeechConstants.ServiceName;
    public string[] Features { get; } = Array.Empty<string>();
    
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
        : base(settingsRepository)
    {
        _logger = loggerFactory.CreateLogger<WindowsSpeechSpeechToText>();
    }
    
    protected override async Task<bool> TryInitializeAsync(WindowsSpeechSettings settings, IPrerequisitesValidator prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;
        
        if (!prerequisites.ValidateFeatures()) return false;
        if (dry) return true;

        _recognizer = new SpeechRecognitionEngine(CultureInfo.GetCultureInfoByIetfLanguageTag(culture));
        var grammar = new DictationGrammar();
        // ReSharper disable once MethodHasAsyncOverload
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
    
    public ValueTask DisposeAsync()
    {
        if(_disposed) return ValueTask.CompletedTask;
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
        return ValueTask.CompletedTask;
    }
}
#endif
