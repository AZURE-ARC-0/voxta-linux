using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Voxta.Services.OpenAI;

namespace Voxta.Services.Oobabooga;

public class OobaboogaTextGenClient : ITextGenService, IActionInferenceService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;
    private bool _initialized;

    public OobaboogaTextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
    {
        _httpClient = httpClientFactory.CreateClient($"{OobaboogaConstants.ServiceName}.TextGen");
        _settingsRepository = settingsRepository;
        _sanitizer = sanitizer;
        _performanceMetrics = performanceMetrics;
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        _initialized = true;
        var settings = await _settingsRepository.GetAsync<OobaboogaSettings>(cancellationToken);
        if (settings == null) throw new OobaboogaException("Text Generation Web UI is not configured.");
        var uri = settings?.Uri;
        if (string.IsNullOrEmpty(uri)) throw new OobaboogaException("Missing uri in settings.");
        _httpClient.BaseAddress = new Uri(uri);
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    public async ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder();
        // TODO: count tokens?
        var prompt = builder.BuildReplyPrompt(chatSessionData, -1);
        
        var text = await SendCompletionRequest(prompt, new[] { "END_OF_DIALOG", "You:", $"{chatSessionData.UserName}:", $"{chatSessionData.Character.Name}:", "\n" }, cancellationToken);
        var sanitized = _sanitizer.Sanitize(text);
        
        return new TextData
        {
            Text = sanitized,
            Tokens = 0,
        };
    }

    public async ValueTask<string> SelectActionAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder();
        var prompt = builder.BuildActionInferencePrompt(chatSessionData);
        
        var animation = await SendCompletionRequest(prompt, new[] { "]" }, cancellationToken);
        return animation.Trim('\'', '"', '.', '[', ']', ' ').ToLowerInvariant();
    }

    private async Task<string> SendCompletionRequest(string prompt, string[] stoppingStrings, CancellationToken cancellationToken)
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

        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new OobaboogaException(await response.Content.ReadAsStringAsync(cancellationToken));

        var json = await response.Content.ReadFromJsonAsync<TextGenResponse>(cancellationToken: cancellationToken);

        textGenPerf.Done();

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