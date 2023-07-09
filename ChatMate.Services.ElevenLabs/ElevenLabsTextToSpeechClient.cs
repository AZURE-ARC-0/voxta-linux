using System.Security.Authentication;
using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ElevenLabs;
using ElevenLabs.Models;
using ElevenLabs.Voices;
using Microsoft.Extensions.Logging;

namespace ChatMate.Services.ElevenLabs;

public class ElevenLabsTextToSpeechClient : ITextToSpeechService
{
    public string ServiceName => ElevenLabsConstants.ServiceName;
    
    private readonly ILogger<ElevenLabsTextToSpeechClient> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IPerformanceMetrics _performanceMetrics;
    private ElevenLabsClient? _api;
    private VoiceSettings? _voiceSettings;
    private Model? _model;

    public ElevenLabsTextToSpeechClient(ISettingsRepository settingsRepository, ILoggerFactory loggerFactory, IPerformanceMetrics performanceMetrics)
    {
        _settingsRepository = settingsRepository;
        _performanceMetrics = performanceMetrics;
        _logger = loggerFactory.CreateLogger<ElevenLabsTextToSpeechClient>();
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<ElevenLabsSettings>(ElevenLabsConstants.ServiceName, cancellationToken);
        if (string.IsNullOrEmpty(settings?.ApiKey)) throw new AuthenticationException("ElevenLabs token is missing.");
        _api = new ElevenLabsClient(settings.ApiKey);
        _voiceSettings = new VoiceSettings(0.4f, 0.8f);
        _model = Model.MultiLingualV1;
    }

    public string[] GetThinkingSpeech()
    {
        return new[]
        {
            "m",
            "uh",
            "..",
            "mmh",
            "hum",
        };
    }

    public async Task<VoiceInfo[]> GetVoicesAsync(CancellationToken cancellationToken)
    {
        if (_api == null) throw new NullReferenceException("InitializeAsync() was not called.");

        var voices = await _api.VoicesEndpoint.GetAllVoicesAsync(cancellationToken);

        return voices.Select(v => new VoiceInfo { Id = v.Id, Label = v.Name }).ToArray();
    }

    public async Task GenerateSpeechAsync(SpeechRequest speechRequest, ISpeechTunnel tunnel, string extension, CancellationToken cancellationToken)
    {
        if (_api == null) throw new NullReferenceException("InitializeAsync() was not called.");

        #warning What about extension? And what about the tunnel? This API is weird. We'll change it because of this.
        var path = Path.GetTempPath();
        string? file = null;
        try
        {
            var ttsPerf = _performanceMetrics.Start("ElevenLabs.TextToSpeech");
            file = await _api.TextToSpeechEndpoint.TextToSpeechAsync(speechRequest.Text, new Voice(speechRequest.Voice), _voiceSettings, _model, path, false, cancellationToken);
            ttsPerf.Done();

            var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
            await tunnel.SendAsync(bytes, "audio/mpeg", cancellationToken);
        }
        finally
        {
            if (file != null)
                File.Delete(file);
        }
    }
}