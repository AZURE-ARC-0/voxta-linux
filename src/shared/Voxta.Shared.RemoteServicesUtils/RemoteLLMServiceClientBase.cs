using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Tokenizers;
using Voxta.Common;
using Voxta.Shared.LLMUtils;

namespace Voxta.Shared.RemoteServicesUtils;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public abstract class RemoteLLMServiceClientBase<TSettings, TInputParameters, TOutputParameters> : LLMServiceClientBase<TSettings, TInputParameters, TOutputParameters>
    where TSettings : RemoteLLMServiceSettingsBase<TInputParameters> where TInputParameters : new()
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    }; 
    
    protected override ITokenizer Tokenizer { get; } = TokenizerFactory.GetDefault();
    
    protected abstract string GenerateRequestPath { get; }
    
    private readonly HttpClient _httpClient;

    protected RemoteLLMServiceClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
        : base(settingsRepository)
    {
        _httpClient = httpClientFactory.CreateClient(GetType().Name);
    }

    protected override async Task<bool> TryInitializeAsync(TSettings settings, string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;
        
        return TryInitializeSync(settings, dry);
    }

    private bool TryInitializeSync(TSettings settings, bool dry)
    {
        var uri = settings.Uri;
        if (string.IsNullOrEmpty(uri)) return false;
        if (dry) return true;

        _httpClient.BaseAddress = new Uri(uri);
        return true;
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
}