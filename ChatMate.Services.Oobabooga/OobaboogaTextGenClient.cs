using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;

namespace ChatMate.Services.Oobabooga;

public class OobaboogaTextGenClient : ITextGenService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;

    public OobaboogaTextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
    {
        _httpClient = httpClientFactory.CreateClient($"{OobaboogaConstants.ServiceName}.TextGen");
        _settingsRepository = settingsRepository;
        _sanitizer = sanitizer;
        _performanceMetrics = performanceMetrics;
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<OobaboogaSettings>(cancellationToken);
        var uri = settings?.Uri;
        if (string.IsNullOrEmpty(uri)) throw new OobaboogaException("Missing uri in settings");
        _httpClient.BaseAddress = new Uri(uri);
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    public async ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        // TODO: count tokens?
        var preamble = MakePreamble(chatSessionData.Character);
        var messages = string.Join("\n", chatSessionData.GetMessages().Select(x => $"{x.User}: \"{x.Text}\""));
        var prompt = $"""
        {preamble}
        {messages}
        {chatSessionData.Character.Name}:
        """.ReplaceLineEndings("\n").Replace("\n\n", "\n");
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
            stopping_strings = new[] { "END_OF_DIALOG", "You:", $"{chatSessionData.UserName}:", $"{chatSessionData.Character.Name}:", "\n" },
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
        var sanitized = _sanitizer.Sanitize(text);
        
        return new TextData
        {
            Text = sanitized,
            Tokens = 0,
        };
    }
    
    private static string MakePreamble(CharacterCard character)
    {
        return $"""
            {character.SystemPrompt}
            Description of {character.Name}: {character.Description}
            Personality of {character.Name}: {character.Personality}
            Circumstances and context of the dialogue: {character.Scenario}
            """;
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
}