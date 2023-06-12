using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Specialized;

namespace ChatMate.Server.Services;

public class NovelAIOptions
{
    public required string Token { get; set; }
}

public class NovelAIClient : ITextGenService, ITextToSpeechService
{
    private ConcurrentDictionary<Guid, string> _pendingSpeechRequests = new();

    private readonly IOptions<NovelAIOptions> _options;
    private readonly IOptions<ChatMateServerOptions> _serverOptions;
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly object _parameters;

    public NovelAIClient(IOptions<NovelAIOptions> options, IOptions<ChatMateServerOptions> serverOptions, IHttpClientFactory httpClientFactory)
    {
        _options = options;
        _serverOptions = serverOptions;
        _httpClient = httpClientFactory.CreateClient("NovelAI");
        _httpClient.BaseAddress = new Uri("https://api.novelai.net");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/event-stream");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.Value.Token}");
        _model = "clio-v1";
        _parameters = new
        {
            temperature = 1.05,
            max_length = 80,
            min_length = 1,
            top_k = 80,
            top_p = 0.95,
            top_a = 0.075,
            tail_free_sampling = 0.967,
            repetition_penalty = 1.5,
            repetition_penalty_range = 8000,
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
                new[]
                {
                    3
                },
                new[]
                {
                    49356
                },
                new[]
                {
                    1431
                },
                new[]
                {
                    31715
                },
                new[]
                {
                    34387
                },
                new[]
                {
                    20765
                },
                new[]
                {
                    30702
                },
                new[]
                {
                    10691
                },
                new[]
                {
                    49333
                },
                new[]
                {
                    1266
                },
                new[]
                {
                    19438
                },
                new[]
                {
                    43145
                },
                new[]
                {
                    26523
                },
                new[]
                {
                    41471
                },
                new[]
                {
                    2936
                },
            },
        };
    }

    public async ValueTask<string> GenerateTextAsync(string text)
    {
        var input = $"""
        This is a conversation between the user and a virtual companion. She is beautiful, and subservient.
        User: {text}
        Kally:
        """;
        var body = new
        {
            model = _model,
            input,
            parameters = _parameters
        };
        var bodyContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/ai/generate-stream");
        request.Content = bodyContent;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception(await response.Content.ReadAsStringAsync());

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        var sb = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;
            if (!line.StartsWith("data:")) continue;
            var json = JsonSerializer.Deserialize<SSEEvent>(line[5..]);
            if (json == null) break;
            if (json.token.Contains('\n')) break;
            sb.Append(json.token);
        }
        reader.Close();
        return sb.ToString();
    }

    public ValueTask<string> GenerateSpeechUrlAsync(string text)
    {
        var id = Guid.NewGuid();
        if (!_pendingSpeechRequests.TryAdd(id, text))
            throw new Exception("Unable to save the speech to the pending requests.");
        return ValueTask.FromResult($"http://{_serverOptions.Value.IpAddress}:{_serverOptions.Value.Port}/speech?id={id}");
    }

    public async Task HandleSpeechRequest(string rawRequest, NetworkStream responseStream)
    {
        var id = Guid.Parse(rawRequest.AsSpan("GET /speech?id=".Length, "aa4f7118-272c-4faa-953e-48de2fa3832d".Length));
        var writer = new StreamWriter(responseStream, Encoding.ASCII, leaveOpen: true);
        
        if(!_pendingSpeechRequests.TryRemove(id, out var text))
        {
            await writer.WriteAsync("HTTP/1.1 404 NotFound\r\n");
            await writer.WriteAsync("Date: " + DateTime.UtcNow.ToString("R") + "\r\n");
            await writer.WriteAsync("Content-Length: 0\r\n");
            await writer.WriteAsync("\r\n");
            await writer.FlushAsync();
            return;
        }
        
        var uriBuilder = new UriBuilder(new Uri(_httpClient.BaseAddress!, "/ai/generate-voice"))
        {
            Query = new NameValueCollection
            {
                ["text"] = text,
                ["voice"] = "-1",
                ["seed"] = "Naia",
                ["opus"] = "true",
                ["version"] = "v2"
            }.ToString()
        };

        var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/webm"));
        using var response = await _httpClient.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        
        await writer.WriteAsync("HTTP/1.1 200 OK\r\n");
        await writer.WriteAsync("\r\n");
        await writer.WriteAsync("Connection: keep-alive\r\n");
        await writer.WriteAsync("Content-Type: audio/webm\r\n");
        await writer.WriteAsync($"Content-Length: {bytes.Length}\r\n");
        await writer.WriteAsync("\r\n");
        await writer.FlushAsync();
        
        await responseStream.WriteAsync(bytes);
        await responseStream.FlushAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            // TODO: Logger
            Console.Error.WriteLine(await response.Content.ReadAsStreamAsync());
        }
    }

    [Serializable]
    private class SSEEvent
    {
        public string token { get; init; }
        public bool final { get; init; }
        public int ptr { get; init; }
        public string error { get; init; }
    }
}
