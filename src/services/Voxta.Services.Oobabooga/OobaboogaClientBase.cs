﻿using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Voxta.Abstractions.Repositories;

namespace Voxta.Services.Oobabooga;

public class OobaboogaClientBase
{
    private static readonly IMapper Mapper;
    
    static OobaboogaClientBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OobaboogaParameters, OobaboogaRequestBody>();
        });
        Mapper = config.CreateMapper();
    }
    
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private OobaboogaParameters? _parameters;
    private bool _initialized;

    protected OobaboogaClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
    {
        _httpClient = httpClientFactory.CreateClient(OobaboogaConstants.ServiceName);
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        if (_initialized) return true;
        _initialized = true;
        var settings = await _settingsRepository.GetAsync<OobaboogaSettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        var uri = settings.Uri;
        if (string.IsNullOrEmpty(uri)) return false;
        _httpClient.BaseAddress = new Uri(uri);
        _parameters = settings.Parameters ?? new OobaboogaParameters();
        return true;
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    protected async Task<string> SendCompletionRequest(string prompt, string[] stoppingStrings, CancellationToken cancellationToken)
    {
        var body = Mapper.Map<OobaboogaRequestBody>(_parameters);
        body.Prompt = prompt;
        body.StoppingStrings = stoppingStrings;
        
        var bodyContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/generate");
        request.Content = bodyContent;

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new OobaboogaException(string.IsNullOrEmpty(errorBody) ? $"Status {response.StatusCode}" : errorBody);
        }

        var json = await response.Content.ReadFromJsonAsync<TextGenResponse>(cancellationToken: cancellationToken);

        var text = json?.results?[0].text ?? throw new OobaboogaException("Missing text in response");
        return text;
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private class TextGenResponse
    {
        public List<TextGenResponseResult>? results { get; init; }
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private class TextGenResponseResult
    {
        public string? text { get; init; }
    }

    public void Dispose()
    {
    }
}