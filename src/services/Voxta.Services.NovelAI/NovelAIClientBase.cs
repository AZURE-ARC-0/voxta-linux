using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;
using Voxta.Common;
using Voxta.Services.NovelAI.Presets;

namespace Voxta.Services.NovelAI;

public class NovelAIClientBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    
    private static readonly IMapper Mapper;
    
    protected static readonly ITokenizer Tokenizer = new NovelAITokenizer();
    
    static NovelAIClientBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NovelAIParameters, NovelAIRequestBodyParameters>();
        });
        Mapper = config.CreateMapper();
    }
    
    public string ServiceName => NovelAIConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };

    protected int MaxContextTokens { get; private set; }
    
    private readonly HttpClient _httpClient;
    private NovelAIParameters? _parameters;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILocalEncryptionProvider _encryptionProvider;
    private string _model = "clio-v1";

    protected NovelAIClientBase(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, ILocalEncryptionProvider encryptionProvider)
    {
        _settingsRepository = settingsRepository;
        _encryptionProvider = encryptionProvider;
        _httpClient = httpClientFactory.CreateClient(NovelAIConstants.ServiceName);
    }
    
    public async Task<bool> TryInitializeAsync(string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<NovelAISettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        if (string.IsNullOrEmpty(settings.Token)) return false;
        if (!culture.StartsWith("en") && !culture.StartsWith("jp")) return false;
        if (prerequisites.Contains(ServiceFeatures.GPT3)) return false;
        if (!ValidateSettings(settings)) return false;
        if (dry) return true;
        
        _httpClient.BaseAddress = new Uri("https://api.novelai.net");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _encryptionProvider.Decrypt(settings.Token));
        _model = settings.Model;
        _parameters = settings.Parameters ?? NovelAIPresets.DefaultForModel(_model);
        MaxContextTokens = settings.MaxContextTokens;
        return true;
    }
    
    protected virtual bool ValidateSettings(NovelAISettings settings)
    {
        return true;
    }

    public int GetTokenCount(string value)
    {
        return Tokenizer.CountTokens(value);
    }

    protected NovelAIRequestBody BuildRequestBody(string prompt, string prefix)
    {
        var parameters = Mapper.Map<NovelAIRequestBodyParameters>(_parameters);
        parameters.Prefix = prefix;

        /*
        // TODO: Add this once I have a NAI tokenizer. Also, most of this can be pre-generate or cached in InitializeAsync.
        var bias = new List<LogitBiasExp>(4)
        {
            new()
            {
                Bias = 2,
                EnsureSequenceFinish = true,
                GenerateOnce = true,
                Sequence = _tokenizer.Encode($"\n{chatSessionData.Character.Name}:")
            },
            new()
            {
                Bias = 0,
                EnsureSequenceFinish = true,
                GenerateOnce = true,
                Sequence = _tokenizer.Encode($"\n{chatSessionData.UserName}: ")
            }
        };
        bias.AddRange(parameters.LogitBiasExp ?? Array.Empty<LogitBiasExp>());
        parameters.LogitBiasExp = bias.ToArray();
        */

        var body = new NovelAIRequestBody
        {
            Model = _model,
            Input = prompt,
            Parameters = parameters
        };
        return body;
    }

    protected async ValueTask<string> SendCompletionRequest(NovelAIRequestBody body, CancellationToken cancellationToken)
    {
        var bodyContent = new StringContent(JsonSerializer.Serialize(body, JsonSerializerOptions), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/ai/generate-stream");
        request.Content = bodyContent;
        request.ConfigureEvenStream();

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new NovelAIException(await response.Content.ReadAsStringAsync(cancellationToken));
        var text = await response.ReadEventStream<NovelAIEventData>(cancellationToken);
        return text.TrimExcess();
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class NovelAIEventData : IEventStreamData
    {
        public required string token { get; init; }
        public bool final { get; init; }
        public int ptr { get; init; }
        public string? error { get; init; }
        
        public string GetToken() => token;
    }

    public void Dispose()
    {
    }
}