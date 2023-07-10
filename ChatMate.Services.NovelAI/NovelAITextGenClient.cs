using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using NAudio.MediaFoundation;

namespace ChatMate.Services.NovelAI;

public class NovelAITextGenClient : ITextGenService
{
    private readonly HttpClient _httpClient;
    private readonly object _parameters;
    private readonly ISettingsRepository _settingsRepository;
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;
    private string _model = "clio-v1";

    static NovelAITextGenClient()
    {
        MediaFoundationApi.Startup();
    }

    public NovelAITextGenClient(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
    {
        _settingsRepository = settingsRepository;
        _sanitizer = sanitizer;
        _performanceMetrics = performanceMetrics;
        _httpClient = httpClientFactory.CreateClient(NovelAIConstants.ServiceName);
        _parameters = new
        {
            temperature = 1.05,
            max_length = 40,
            min_length = 1,
            top_k = 80,
            top_p = 0.95,
            top_a = 0.075,
            tail_free_sampling = 0.967,
            repetition_penalty = 1.5,
            repetition_penalty_range = 8192,
            repetition_penalty_slope = 0.09,
            repetition_penalty_frequency = 0.03,
            repetition_penalty_presence = 0.005,
            generate_until_sentence = true,
            use_cache = false,
            use_string = true,
            return_full_text = false,
            prefix = "vanilla",
            order = new[] { 1, 3, 4, 0, 2 },
            bad_words_ids = new[]
            {
                new[] { 3 },
                new[] { 49356 },
                new[] { 1431 },
                new[] { 31715 },
                new[] { 34387 },
                new[] { 20765 },
                new[] { 30702 },
                new[] { 10691 },
                new[] { 49333 },
                new[] { 1266 },
                new[] { 19438 },
                new[] { 43145 },
                new[] { 26523 },
                new[] { 41471 },
                new[] { 2936 },
            },
            stop_sequences = new[]
            {
                // TODO: This is User: and " (this may not be the most efficient way to go)
                new[]{ 49287 },
                new[]{ 49264 }
            }
        };
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<NovelAISettings>(NovelAIConstants.ServiceName);
        _httpClient.BaseAddress = new Uri("https://api.novelai.net");
        if (string.IsNullOrEmpty(settings?.Token)) throw new AuthenticationException("NovelAI token is missing.");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Crypto.DecryptString(settings.Token));
        _model = settings.Model;
    }

    public async ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        // TODO: count tokens: https://novelai.net/tokenizer
        // TODO: Move the settings to appsettings
        var chatMessages = chatSessionData.GetMessages();
        var input = $"""
        Scenario: {chatSessionData.Preamble.Text}
        {string.Join("\n", chatMessages.Select(x => $"{x.User}: \"{x.Text}\""))}
        {chatSessionData.BotName}: \"
        """.ReplaceLineEndings("\n");
        var body = new
        {
            model = _model,
            input,
            parameters = _parameters
        };
        var bodyContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/ai/generate-stream");
        request.Content = bodyContent;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var textGenPerf = _performanceMetrics.Start("NovelAI.TextGen");
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new NovelAIException(await response.Content.ReadAsStringAsync(cancellationToken));

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        var sb = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (!line.StartsWith("data:")) continue;
            var json = JsonSerializer.Deserialize<NovelAIEventData>(line[5..]);
            if (json == null) break;
            // TODO: Determine which tokens are considered end tokens.
            var token = json.token;
            if (token.Contains('\n') || token.Contains('\"')) break;
            sb.Append(token);
            // TODO: Determine a rule of thumb for when to stop.
            // if (sb.Length > 40 && json.token.Contains('.') || json.token.Contains('!') || json.token.Contains('?')) break;
        }
        reader.Close();
        
        textGenPerf.Done();

        var text = sb.ToString();
        var sanitized = _sanitizer.Sanitize(text);
        
        return new TextData
        {
            Text = sanitized,
            Tokens = 0,
        };
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private class NovelAIEventData
    {
        public required string token { get; init; }
        public bool final { get; init; }
        public int ptr { get; init; }
        public string? error { get; init; }
    }
}
