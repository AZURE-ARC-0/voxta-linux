﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.CognitiveServices.Speech;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Voxta.Abstractions;
using Voxta.Abstractions.System;
using VoiceInfo = Voxta.Abstractions.Model.VoiceInfo;

namespace Voxta.Services.AzureSpeechService;

public class AzureSpeechServiceTextToSpeech : ServiceBase<AzureSpeechServiceSettings>, ITextToSpeechService
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")] private static readonly string[] RestrictedVoices = new[]
    {
        "de-DE-GiselaNeural",
        "en-AU-CarlyNeural",
        "en-GB-MaisieNeural",
        "en-US-AnaNeural",
        "es-ES-IreneNeural",
        "es-MX-MarinaNeural",
        "fr-FR-EloiseNeural",
        "it-IT-PierinaNeural",
        "ja-JP-AoiNeural",
        "pt-BR-LeticiaNeural",
        "zh-CN-XiaoshuangNeural",
        "zh-CN-XiaoyouNeural",
    };

    protected override string ServiceName => AzureSpeechServiceConstants.ServiceName;
    public string[] Features { get; private set; } = Array.Empty<string>();

    private readonly ILogger<AzureSpeechServiceTextToSpeech> _logger;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly ILocalEncryptionProvider _encryptionProvider;
    private SpeechSynthesizer? _speechSynthesizer;
    private string _culture = "en-US";

    public AzureSpeechServiceTextToSpeech(ISettingsRepository settingsRepository, ILoggerFactory loggerFactory, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider)
        : base(settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
        _encryptionProvider = encryptionProvider;
        _logger = loggerFactory.CreateLogger<AzureSpeechServiceTextToSpeech>();
    }
    
    protected override async Task<bool> TryInitializeAsync(AzureSpeechServiceSettings settings, IPrerequisitesValidator prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;
        
        if (string.IsNullOrEmpty(settings.SubscriptionKey)) return false;
        if (string.IsNullOrEmpty(settings.Region)) return false;
        if (!prerequisites.ValidateFeatures(settings.FilterProfanity
                ? Array.Empty<string>()
                : new[] { ServiceFeatures.NSFW }))
            return false;
        if (dry) return true;
        
        var config = SpeechConfig.FromSubscription(_encryptionProvider.Decrypt(settings.SubscriptionKey), settings.Region);
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
        var voice = GetVoice(speechRequest);
        var ssml = $"""
            <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="{speechRequest.Culture}">
                <voice name="{voice}">{speechRequest.Text}</voice>
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

    private string GetVoice(SpeechRequest speechRequest)
    {
        if (string.IsNullOrEmpty(speechRequest.Voice) || speechRequest.Voice == SpecialVoices.Female)
            return  _culture == "en-US" ? "en-US-JennyNeural" : "en-US-JennyMultilingualNeural";
        if (speechRequest.Voice == SpecialVoices.Male)
            return  "en-US-GuyNeural";
        return speechRequest.Voice;
    }

    public async Task<VoiceInfo[]> GetVoicesAsync(CancellationToken cancellationToken)
    {
        if (_speechSynthesizer == null) throw new NullReferenceException("AzureSpeechService is not initialized");
        var response = await _speechSynthesizer.GetVoicesAsync(_culture);
        if (response == null) throw new NullReferenceException("No voices returned");
        return response.Voices
            .Where(v => !RestrictedVoices.Contains(v.ShortName))
            .Select(v => new VoiceInfo
            {
                Id = v.ShortName,
                Label = $"{v.LocalName} ({v.Gender})",
            })
            .ToArray();
    }

    
    public ValueTask DisposeAsync()
    {
        _speechSynthesizer?.Dispose();
        return ValueTask.CompletedTask;
    }
}
