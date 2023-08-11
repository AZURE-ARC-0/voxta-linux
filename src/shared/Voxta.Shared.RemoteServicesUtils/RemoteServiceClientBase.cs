using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Tokenizers;
using Voxta.Common;
using Voxta.Services.KoboldAI;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Shared.RemoteServicesUtils;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public abstract class RemoteServiceClientBase<TSettings, TInputParameters, TOutputParameters>
    where TSettings : RemoteServiceSettingsBase<TInputParameters> where TInputParameters : new()
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    }; 
    
    private static readonly IMapper Mapper;
    
    protected static readonly ITokenizer Tokenizer = TokenizerFactory.GetDefault();
    
    static RemoteServiceClientBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TInputParameters, TOutputParameters>();
        });
        Mapper = config.CreateMapper();
    }


    public string ServiceName { get; }
    public string[] Features => new[] { ServiceFeatures.NSFW, ServiceFeatures.GPT3 };

    protected int MaxMemoryTokens { get; private set; }
    protected int MaxContextTokens { get; private set; }
    
    protected abstract string GenerateRequestPath { get; }
    protected abstract bool Streaming { get; }
    
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private TInputParameters? _parameters;
    private bool _initialized;

    protected RemoteServiceClientBase(string serviceName, IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
    {
        ServiceName = serviceName;
        _httpClient = httpClientFactory.CreateClient(serviceName);
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> TryInitializeAsync(string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        if (_initialized) return true;
        _initialized = true;
        var settings = await _settingsRepository.GetAsync<TSettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false; 
        var uri = settings.Uri;
        if (string.IsNullOrEmpty(uri)) return false;
        if (dry) return true;
        
        _httpClient.BaseAddress = new Uri(uri);
        _parameters = settings.Parameters ?? new TInputParameters();
        MaxMemoryTokens = settings.MaxMemoryTokens;
        MaxContextTokens = settings.MaxContextTokens;
        return true;
    }

    public int GetTokenCount(string message)
    {
        return Tokenizer.CountTokens(message);
    }

    protected TOutputParameters CreateParameters()
    {
        return Mapper.Map<TOutputParameters>(_parameters);
    }

    protected async Task<string> SendStreamingCompletionRequest<TEventData>(object body, CancellationToken cancellationToken)
        where TEventData : IEventStreamData
    {
        var bodyContent = new StringContent(JsonSerializer.Serialize(body, JsonSerializerOptions), Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, GenerateRequestPath);
        request.Content = bodyContent;
        request.ConfigureEventStream();
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new RemoteServiceException(await response.Content.ReadAsStringAsync(cancellationToken));

        var text = await response.ReadEventStream<TEventData>(cancellationToken);
        return text.TrimExcess();
    }

    protected async Task<TResponse> SendCompletionRequest<TResponse>(object body, CancellationToken cancellationToken)
    {
        var bodyContent = new StringContent(JsonSerializer.Serialize(body, JsonSerializerOptions), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, GenerateRequestPath);
        request.Content = bodyContent;

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new RemoteServiceException(string.IsNullOrEmpty(errorBody) ? $"Status {response.StatusCode}" : errorBody);
        }

        var json = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
        if (json == null) throw new RemoteServiceException("Could not deserialize response");
        return json;
    }

    public void Dispose()
    {
    }
}