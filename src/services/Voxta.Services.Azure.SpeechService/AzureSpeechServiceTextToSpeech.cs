using Microsoft.CognitiveServices.Speech;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Microsoft.Extensions.Logging;
using VoiceInfo = Voxta.Abstractions.Model.VoiceInfo;

namespace Voxta.Services.AzureSpeechService;

public class AzureSpeechServiceTextToSpeech : ITextToSpeechService
{
    public string ServiceName => AzureSpeechServiceConstants.ServiceName;
    
    public string[] Features { get; private set; } = Array.Empty<string>();

    private readonly ILogger<AzureSpeechServiceTextToSpeech> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IPerformanceMetrics _performanceMetrics;
    private SpeechSynthesizer? _speechSynthesizer;
    private string _culture = "en-US";

    public AzureSpeechServiceTextToSpeech(ISettingsRepository settingsRepository, ILoggerFactory loggerFactory, IPerformanceMetrics performanceMetrics)
    {
        _settingsRepository = settingsRepository;
        _performanceMetrics = performanceMetrics;
        _logger = loggerFactory.CreateLogger<AzureSpeechServiceTextToSpeech>();
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
            var filename = DateTimeOffset.Now.ToString("yy-MM-dd") + "_tts_" + Path.GetFileName(settings.LogFilename);
            config.SetProperty(PropertyId.Speech_LogFilename, Path.Combine(directory, filename));
        }
        
        _speechSynthesizer = new SpeechSynthesizer(config, null);
        _culture = culture;
        return true;
    }

    public string ContentType => "audio/x-wav";

    public string[] GetThinkingSpeech()
    {
        return Array.Empty<string>();
    }

    public async Task GenerateSpeechAsync(SpeechRequest speechRequest, ISpeechTunnel tunnel, CancellationToken cancellationToken)
    {
        if (_speechSynthesizer == null) throw new NullReferenceException("AzureSpeechService is not initialized");
        var ttsPerf = _performanceMetrics.Start("AzureSpeechService.TextToSpeech");
        var ssml = $"""
            <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="{speechRequest.Culture}">
                <voice name="{speechRequest.Voice}">{speechRequest.Text}</voice>
            </speak>
            """;
        var result = await _speechSynthesizer.SpeakSsmlAsync(ssml);

        if (result.Reason != ResultReason.SynthesizingAudioCompleted)
        {
            _logger.LogError("AzureSpeechService failed to generate speech: {Reason}", result.Reason);
            await tunnel.ErrorAsync(new AzureSpeechServiceException($"Unable to generate speech: {result.Reason}"), cancellationToken);
            return;
        }

        var bytes = result.AudioData;
        await using var stream = new MemoryStream(bytes);
        await tunnel.SendAsync(new AudioData(stream, "audio/x-wav"), cancellationToken);
        ttsPerf.Done();
    }

    public async Task<VoiceInfo[]> GetVoicesAsync(CancellationToken cancellationToken)
    {
        if (_speechSynthesizer == null) throw new NullReferenceException("AzureSpeechService is not initialized");
        var response = await _speechSynthesizer.GetVoicesAsync(_culture);
        if (response == null) throw new NullReferenceException("No voices returned");
        return response.Voices.Select(v => new VoiceInfo { Id = v.ShortName, Label = $"{v.ShortName} ({v.Gender})" }).ToArray();
    }

    public void Dispose()
    {
        _speechSynthesizer?.Dispose();
    }
}