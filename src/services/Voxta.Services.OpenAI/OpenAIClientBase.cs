using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Voxta.Abstractions.Repositories;
using Voxta.Common;

namespace Voxta.Services.OpenAI;

public abstract class OpenAIClientBase
{
    private static readonly IMapper Mapper;
    
    static OpenAIClientBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OpenAIParameters, OpenAIRequestBody>();
        });
        Mapper = config.CreateMapper();
    }
    
    private readonly ISettingsRepository _settingsRepository;
    private readonly HttpClient _httpClient;
    private OpenAIParameters? _parameters;
    private string _model = "gpt-3.5-turbo";

    protected OpenAIClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
        _httpClient = httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}");
    }

    public virtual async Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<OpenAISettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        if (string.IsNullOrEmpty(settings.ApiKey)) return false;
        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",  Crypto.DecryptString(settings.ApiKey));
        _model = settings.Model;
        _parameters = new OpenAIParameters();
        return true;
    }
    
    protected async Task<string> SendChatRequestAsync(List<OpenAIMessage> messages, CancellationToken cancellationToken)
    {
        var body = Mapper.Map<OpenAIRequestBody>(_parameters);
        body.Messages = messages;
        body.Model = _model;

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new OpenAIException(await response.Content.ReadAsStringAsync(cancellationToken));

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var apiResponse = (JsonElement?)await JsonSerializer.DeserializeAsync<dynamic>(stream, cancellationToken: cancellationToken);

        if (apiResponse == null) throw new NullReferenceException("OpenAI API response was null");

        return apiResponse.Value.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    public void Dispose()
    {
    }
}