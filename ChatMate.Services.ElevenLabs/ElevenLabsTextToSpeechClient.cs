using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Authentication;
using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Services.ElevenLabs;

public class ElevenLabsTextToSpeechClient : ITextToSpeechService
{
    public string ServiceName => ElevenLabsConstants.ServiceName;
    
    private readonly ILogger<ElevenLabsTextToSpeechClient> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly HttpClient _httpClient;

    public ElevenLabsTextToSpeechClient(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IPerformanceMetrics performanceMetrics)
    {
        _settingsRepository = settingsRepository;
        _performanceMetrics = performanceMetrics;
        _logger = loggerFactory.CreateLogger<ElevenLabsTextToSpeechClient>();
        _httpClient = httpClientFactory.CreateClient($"{ElevenLabsConstants.ServiceName}.TextToSpeech");
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _httpClient.BaseAddress = new Uri("https://api.elevenlabs.io");
        var settings = await _settingsRepository.GetAsync<ElevenLabsSettings>(cancellationToken);
        if (string.IsNullOrEmpty(settings?.ApiKey)) throw new AuthenticationException("ElevenLabs token is missing.");
        _httpClient.DefaultRequestHeaders.Add("xi-api-key", Crypto.DecryptString(settings.ApiKey));
    }

    public string ContentType => "audio/mpeg";

    public string[] GetThinkingSpeech()
    {
        return new[]
        {
            "mh",
            "..",
            "mmh",
            "hum",
        };
    }

    public async Task GenerateSpeechAsync(SpeechRequest speechRequest, ISpeechTunnel tunnel, CancellationToken cancellationToken)
    {
        var body = new
        {
            text = speechRequest.Text,
            model_id = "eleven_multilingual_v1",
            voice_settings = new
            {
                stability = 0.45,
                similarity_boost = 0.75,
            }
        };
        var ttsPerf = _performanceMetrics.Start("ElevenLabs.TextToSpeech");
        var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/text-to-speech/{speechRequest.Voice}")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/*"));
        request.Headers.TransferEncodingChunked = false;
        
        var response = await _httpClient.SendAsync(request, cancellationToken);

        
        if (!response.IsSuccessStatusCode)
        {
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to generate speech: {Reason}", reason);
            await tunnel.ErrorAsync($"Unable to generate speech: {reason}", cancellationToken);
            return;
        }
        
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await tunnel.SendAsync(new AudioData(stream, response.Content.Headers.ContentType?.MediaType ?? "audio/webm"), cancellationToken);
        ttsPerf.Done();
    }

    public async Task<VoiceInfo[]> GetVoicesAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetFromJsonAsync<VoicesResponse>("/v1/voices", cancellationToken);
        if (response == null) throw new NullReferenceException("No voices returned");
        return response.voices.Select(v => new VoiceInfo { Id = v.voice_id, Label = v.name }).ToArray();
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class VoicesResponse
    {
        public required VoiceResponse[] voices { get; init; }
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class VoiceResponse
    {
        public required string voice_id { get; init; }
        public required string name { get; init; }
    }
}