using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using ChatMate.Common;

namespace ChatMate.Services.OpenAI;

public abstract class OpenAIClientBase
{
    private readonly HttpClient _httpClient;
    private string _model = "gpt-3.5-turbo";

    protected OpenAIClientBase(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}");
    }

    protected void InitializeClient([NotNull] OpenAISettings? settings)
    {
        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        if (string.IsNullOrEmpty(settings?.ApiKey)) throw new AuthenticationException("OpenAI api key is missing.");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",  Crypto.DecryptString(settings.ApiKey));
        _model = settings.Model;
    }
    
    protected async Task<string> SendChatRequestAsync(List<object> messages, CancellationToken cancellationToken)
    {
        var body = new
        {
            model = _model,
            messages,
            max_tokens = 120,
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new OpenAIException(await response.Content.ReadAsStringAsync(cancellationToken));

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var apiResponse = (JsonElement?)await JsonSerializer.DeserializeAsync<dynamic>(stream, cancellationToken: cancellationToken);

        if (apiResponse == null) throw new NullReferenceException("OpenAI API response was null");

        return apiResponse.Value.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}