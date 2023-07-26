using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Voxta.Abstractions.Repositories;

namespace Voxta.Services.Oobabooga;

public class OobaboogaClientBase
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
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
        return true;
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    protected async Task<string> SendCompletionRequest(string prompt, string[] stoppingStrings, CancellationToken cancellationToken)
    {
        var body = new
        {
            preset = "None",
            max_new_tokens = 80,
            do_sample = true,
            temperature = 0.7,
            top_p = 0.5,
            typical_p = 1,
            tfs = 1,
            top_a = 0,
            repetition_penalty = 1.18,
            encoder_repetition_penalty = 1,
            repetition_penalty_range = 0,
            top_k = 40,
            min_length = 1,
            no_repeat_ngram_size = 0,
            num_beams = 1,
            penalty_alpha = 0,
            length_penalty = 1,
            early_stopping = true,
            seed = -1,
            add_bos_token = false,
            truncation_length = 2048,
            ban_eos_token = false,
            skip_special_tokens = true,
            stopping_strings = stoppingStrings,
            prompt
        };
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