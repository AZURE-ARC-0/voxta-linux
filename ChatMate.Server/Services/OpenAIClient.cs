using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ChatMate.Server.Services;

public class OpenAIOptions
{
    public required string OrganizationId { get; init; }
    public required string ApiKey { get; init; }
    public required string Model { get; init; }
}

public class OpenAIClient : ITextGenService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;

    public OpenAIClient(HttpClient httpClient, IOptions<OpenAIOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async ValueTask<string> GenerateTextAsync(ChatData chatData, string text)
    {
        var messages = new List<object> { new { role = "system", content = chatData.Preamble } };
        foreach (var message in chatData.Messages)
        {
            messages.Add(new { role = message.User, content = message.Text });
        }

        messages.Add(new { role = "user", content = text });

        var body = new
        {
            model = _options.Model,
            messages
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/v1/chat/completions", content);

        response.EnsureSuccessStatusCode();

        var apiResponse = await JsonSerializer.DeserializeAsync<dynamic>(await response.Content.ReadAsStreamAsync());

        return apiResponse!.choices[0].message.content.ToString();
    }
}
