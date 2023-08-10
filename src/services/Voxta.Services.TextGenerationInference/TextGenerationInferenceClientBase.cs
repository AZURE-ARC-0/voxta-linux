using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Tokenizers;
using Voxta.Common;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.TextGenerationInference;

public class TextGenerationInferenceClientBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    
    private static readonly IMapper Mapper;
    
    protected static readonly ITokenizer Tokenizer = TokenizerFactory.GetDefault();
    
    static TextGenerationInferenceClientBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TextGenerationInferenceParameters, TextGenerationInferenceParametersBody>();
        });
        Mapper = config.CreateMapper();
    }
    
    public string ServiceName => TextGenerationInferenceConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };

    protected int MaxMemoryTokens { get; private set; }
    protected int MaxContextTokens { get; private set; }
    
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private TextGenerationInferenceParameters? _parameters;
    private bool _initialized;

    protected TextGenerationInferenceClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
    {
        _httpClient = httpClientFactory.CreateClient(TextGenerationInferenceConstants.ServiceName);
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> TryInitializeAsync(string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        if (_initialized) return true;
        _initialized = true;
        var settings = await _settingsRepository.GetAsync<TextGenerationInferenceSettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        var uri = settings.Uri;
        if (string.IsNullOrEmpty(uri)) return false;
        if (dry) return true;
        
        _httpClient.BaseAddress = new Uri(uri);
        _parameters = settings.Parameters ?? new TextGenerationInferenceParameters();
        MaxMemoryTokens = settings.MaxMemoryTokens;
        MaxContextTokens = settings.MaxContextTokens;
        return true;
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    protected TextGenerationInferenceRequestBody BuildRequestBody(string prompt, string[] stoppingStrings)
    {
        var parameters = Mapper.Map<TextGenerationInferenceParametersBody>(_parameters);
        parameters.Stop = stoppingStrings;
        return new TextGenerationInferenceRequestBody
        {
            Parameters = parameters,
            Inputs = prompt,
        };
    }

    protected async Task<string> SendCompletionRequest(TextGenerationInferenceRequestBody body, CancellationToken cancellationToken)
    {
        var bodyContent = new StringContent(JsonSerializer.Serialize(body, JsonSerializerOptions), Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/generate_stream");
        request.Content = bodyContent;
        request.ConfigureEvenStream();
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new TextGenerationInferenceException(await response.Content.ReadAsStringAsync(cancellationToken));

        var text = await response.ReadEventStream<TextGenEventData>(cancellationToken);
        return text.TrimExcess();
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class TextGenEventData : IEventStreamData
    {
        public required TextGenEventDataToken token { get; init; }
        public string? generated_text { get; init; }
        public string? details { get; init; }
        public string GetToken() => token.GetToken();
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    private class TextGenEventDataToken : IEventStreamData
    {
        public int id { get; init; }
        public required string text { get; init; }
        public double logprob { get; init; }
        public bool special { get; init; }
        public string GetToken() => text;
    }

    public void Dispose()
    {
    }
}