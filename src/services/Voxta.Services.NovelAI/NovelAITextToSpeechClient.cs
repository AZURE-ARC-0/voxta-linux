using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Authentication;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Microsoft.Extensions.Logging;

namespace Voxta.Services.NovelAI;

public class NovelAITextToSpeechClient : ITextToSpeechService
{
    public string ServiceName => NovelAIConstants.ServiceName;
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<NovelAITextToSpeechClient> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IPerformanceMetrics _performanceMetrics;

    public NovelAITextToSpeechClient(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IPerformanceMetrics performanceMetrics)
    {
        _settingsRepository = settingsRepository;
        _performanceMetrics = performanceMetrics;
        _logger = loggerFactory.CreateLogger<NovelAITextToSpeechClient>();
        _httpClient = httpClientFactory.CreateClient($"{NovelAIConstants.ServiceName}.TextToSpeech");
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<NovelAISettings>(cancellationToken);
        _httpClient.BaseAddress = new Uri("https://api.novelai.net");
        if (string.IsNullOrEmpty(settings?.Token)) throw new AuthenticationException("NovelAI token is missing.");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Crypto.DecryptString(settings.Token));
    }

    public string ContentType => "audio/webm";

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

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public Task<VoiceInfo[]> GetVoicesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new VoiceInfo[]
        {
            new() { Id = "Ligeia", Label = "Ligeia (Unisex)" },
            new() { Id = "Aini", Label = "Aini (Female)" },
            new() { Id = "Orea", Label = "Orea (Female)" },
            new() { Id = "Claea", Label = "Claea (Female)" },
            new() { Id = "Lim", Label = "Lim (Female)" },
            new() { Id = "Orae", Label = "Orae (Female)" },
            new() { Id = "Naia", Label = "Naia (Female)" },
            new() { Id = "Olon", Label = "Olon (Male)" },
            new() { Id = "Elei", Label = "Elei (Male)" },
            new() { Id = "Ogma", Label = "Ogma (Male)" },
            new() { Id = "Reid", Label = "Reid (Male)" },
            new() { Id = "Pega", Label = "Pega (Male)" },
            new() { Id = "Lam", Label = "Lam (Male)" },
        });
    }

    public async Task GenerateSpeechAsync(SpeechRequest speechRequest, ISpeechTunnel tunnel, CancellationToken cancellationToken)
    {
        var querystring = new Dictionary<string, string>
        {
            ["text"] = speechRequest.Text,
            ["voice"] = "-1",
            ["seed"] = speechRequest.Voice,
            ["opus"] = "true",
            ["version"] = "v2"
        };
        var uriBuilder = new UriBuilder(new Uri(_httpClient.BaseAddress!, "/ai/generate-voice"))
        {
            Query = await new FormUrlEncodedContent(querystring).ReadAsStringAsync(cancellationToken)
        };

        using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/*"));
        request.Headers.TransferEncodingChunked = false;
        var ttsPerf = _performanceMetrics.Start("NovelAI.TextToSpeech");
        using var audioResponse = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        
        if (!audioResponse.IsSuccessStatusCode)
        {
            var reason = await audioResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("NovelAI failed to generate speech: {Reason}", reason);
            await tunnel.ErrorAsync(new NovelAIException($"Unable to generate speech: {reason}"), cancellationToken);
            return;
        }

        // TODO: Optimize later (we're forced to use a temp file because of the MediaFoundationReader)
        await using var stream = await audioResponse.Content.ReadAsStreamAsync(cancellationToken);
        await tunnel.SendAsync(new AudioData(stream, audioResponse.Content.Headers.ContentType?.MediaType ?? "audio/webm"), cancellationToken);
        ttsPerf.Done();
    }
}