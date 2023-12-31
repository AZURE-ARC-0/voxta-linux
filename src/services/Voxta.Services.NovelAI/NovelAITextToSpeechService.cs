﻿using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Voxta.Abstractions;
using Voxta.Abstractions.System;
using Voxta.Shared.TextToSpeechUtils;

namespace Voxta.Services.NovelAI;

public class NovelAITextToSpeechService : ServiceBase<NovelAISettings>, ITextToSpeechService
{
    protected override string ServiceName => NovelAIConstants.ServiceName;

    public string ContentType => "audio/webm";
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<NovelAITextToSpeechService> _logger;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly ITextToSpeechPreprocessor _preprocessor;
    private readonly ILocalEncryptionProvider _encryptionProvider;
    private string[]? _thinkingSpeech;

    public NovelAITextToSpeechService(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IPerformanceMetrics performanceMetrics, ITextToSpeechPreprocessor preprocessor, ILocalEncryptionProvider encryptionProvider)
        : base(settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
        _preprocessor = preprocessor;
        _encryptionProvider = encryptionProvider;
        _logger = loggerFactory.CreateLogger<NovelAITextToSpeechService>();
        _httpClient = httpClientFactory.CreateClient($"{NovelAIConstants.ServiceName}.TextToSpeech");
    }

    protected override async Task<bool> TryInitializeAsync(NovelAISettings settings, IPrerequisitesValidator prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;
        
        if (string.IsNullOrEmpty(settings.Token)) return false;
        if (!prerequisites.ValidateFeatures(ServiceFeatures.NSFW)) return false;
        if (!prerequisites.ValidateCulture("en", "jp")) return false;
        if (dry) return true;
        
        _httpClient.BaseAddress = new Uri("https://api.novelai.net");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _encryptionProvider.Decrypt(settings.Token));
        _thinkingSpeech = settings.ThinkingSpeech;
        return true;
    }

    public string[] GetThinkingSpeech()
    {
        return _thinkingSpeech ?? Array.Empty<string>();
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
            new() { Id = "Aulon", Label = "Aulon (Male)" },
            new() { Id = "Elei", Label = "Elei (Male)" },
            new() { Id = "Ogma", Label = "Ogma (Male)" },
            new() { Id = "Reid", Label = "Reid (Male)" },
            new() { Id = "Pega", Label = "Pega (Male)" },
            new() { Id = "Lam", Label = "Lam (Male)" },
        });
    }

    public async Task GenerateSpeechAsync(SpeechRequest speechRequest, ISpeechTunnel tunnel, CancellationToken cancellationToken)
    {
        var voice = GetVoice(speechRequest);
        var querystring = new Dictionary<string, string>
        {
            ["text"] = _preprocessor.Preprocess(speechRequest.Text, speechRequest.Culture),
            ["voice"] = "-1",
            ["seed"] = voice,
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
        
        if (audioResponse.Content.Headers.ContentType?.MediaType != "audio/webm")
        {
            var reason = await audioResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("NovelAI generated unexpected audio type: {ContentType}. Body: {Body}", audioResponse.Content.Headers.ContentType?.MediaType ?? "NULL", reason);
            await tunnel.ErrorAsync(new NovelAIException($"Unable to generate speech: {reason}"), cancellationToken);
            return;
        }
        
        await using var stream = await audioResponse.Content.ReadAsStreamAsync(cancellationToken);
        await tunnel.SendAsync(new AudioData(stream, audioResponse.Content.Headers.ContentType?.MediaType ?? "audio/webm"), cancellationToken);
        ttsPerf.Done();
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private string GetVoice(SpeechRequest speechRequest)
    {
        if (string.IsNullOrEmpty(speechRequest.Voice))
            return "Ligeia";
        if(speechRequest.Voice == SpecialVoices.Female)
            return "Aini";
        if (speechRequest.Voice == SpecialVoices.Male)
            return "Aulon";
        return speechRequest.Voice;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
