using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;

namespace Voxta.Services.OpenAI;

public abstract class OpenAIClientBase
{
    protected static readonly ITokenizer Tokenizer = DeepDevTokenizer.Create();
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    
    private static readonly IMapper Mapper;
    
    static OpenAIClientBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OpenAIParameters, OpenAIRequestBody>();
        });
        Mapper = config.CreateMapper();
    }
    
    public string ServiceName => OpenAIConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.GPT3 };

    protected int MaxMemoryTokens { get; private set; }
    protected int MaxContextTokens { get; private set; }
    
    protected OpenAIParameters? Parameters;
    
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILocalEncryptionProvider _encryptionProvider;
    private readonly HttpClient _httpClient;
    private string _model = "gpt-3.5-turbo";

    protected OpenAIClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ILocalEncryptionProvider encryptionProvider)
    {
        _settingsRepository = settingsRepository;
        _encryptionProvider = encryptionProvider;
        _httpClient = httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}");
    }

    public int GetTokenCount(string message)
    {
        return Tokenizer.CountTokens(message);
    }

    public virtual async Task<bool> TryInitializeAsync(string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<OpenAISettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        if (string.IsNullOrEmpty(settings.ApiKey)) return false;
        if (dry) return true;
        
        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",  _encryptionProvider.Decrypt(settings.ApiKey));
        _model = settings.Model;
        Parameters = new OpenAIParameters();
        MaxMemoryTokens = settings.MaxMemoryTokens;
        MaxContextTokens = settings.MaxContextTokens;
        return true;
    }

    protected OpenAIRequestBody BuildRequestBody(List<OpenAIMessage> messages)
    {
        var body = Mapper.Map<OpenAIRequestBody>(Parameters);
        body.Messages = messages;
        body.Model = _model;
        return body;
    }

    protected async Task<string> SendChatRequestAsync(OpenAIRequestBody body, CancellationToken cancellationToken)
    {
        var content = new StringContent(JsonSerializer.Serialize(body, JsonSerializerOptions), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new OpenAIException(await response.Content.ReadAsStringAsync(cancellationToken));

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var apiResponse = (JsonElement?)await JsonSerializer.DeserializeAsync<dynamic>(stream, cancellationToken: cancellationToken);

        if (apiResponse == null) throw new NullReferenceException("OpenAI API response was null");

        return apiResponse.Value.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.TrimExcess() ?? throw new OpenAIException("No content in response: " + apiResponse);
    }

    public void Dispose()
    {
    }
}