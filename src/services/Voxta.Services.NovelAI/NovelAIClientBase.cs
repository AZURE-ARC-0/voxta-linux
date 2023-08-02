using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.System;
using Voxta.Common;
using Voxta.Services.NovelAI.Presets;

namespace Voxta.Services.NovelAI;

public class NovelAIClientBase
{
    private static readonly JsonSerializerOptions JSONSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    
    private static readonly IMapper Mapper;
    
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
    
    public async Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<NovelAISettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        if (string.IsNullOrEmpty(settings.Token)) return false;
        if (!culture.StartsWith("en") && !culture.StartsWith("jp")) return false;
        if (prerequisites.Contains(ServiceFeatures.GPT3)) return false;
        if (!ValidateSettings(settings)) return false;
        _httpClient.BaseAddress = new Uri("https://api.novelai.net");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _encryptionProvider.Decrypt(settings.Token));
        _model = settings.Model;
        _parameters = settings.Parameters ?? NovelAIPresets.DefaultForModel(_model);
        return true;
    }
    
    protected virtual bool ValidateSettings(NovelAISettings settings)
    {
        return true;
    }

    protected async ValueTask<string> SendCompletionRequest(string prompt, string prefix, CancellationToken cancellationToken)
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
        
        var body = new
        {
            model = _model,
            input = prompt,
            parameters
        };
        var bodyContent = new StringContent(JsonSerializer.Serialize(body, JSONSerializerOptions), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/ai/generate-stream");
        request.Content = bodyContent;
        request.ConfigureEvenStream();

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new NovelAIException(await response.Content.ReadAsStringAsync(cancellationToken));
        var text = await response.ReadEventStream<NovelAIEventData>(cancellationToken);
        return text;
    }
    
    public int GetTokenCount(string message)
    {
        return 0;
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