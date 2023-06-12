using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace ChatMate.Server.Services;

public class NovelAIOptions
{
    public required string Token { get; set; }
}

public class NovelAIClient : ITextGenService, ITextToSpeechService
{
    private readonly ConcurrentDictionary<Guid, string> _pendingSpeechRequests = new();

    private readonly IOptions<ChatMateServerOptions> _serverOptions;
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly object _parameters;
    
    static NovelAIClient()
    {
        MediaFoundationApi.Startup();
    }

    public NovelAIClient(IOptions<NovelAIOptions> options, IOptions<ChatMateServerOptions> serverOptions, IHttpClientFactory httpClientFactory)
    {
        _serverOptions = serverOptions;
        _httpClient = httpClientFactory.CreateClient("NovelAI");
        _httpClient.BaseAddress = new Uri("https://api.novelai.net");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/event-stream");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Value.Token}");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/ai/generate-stream");
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
            var json = JsonSerializer.Deserialize<NovelAIEventData>(line[5..]);
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
        // TODO: Instead return a relative URL and let the client join
        return ValueTask.FromResult($"http://{_serverOptions.Value.IpAddress}:{_serverOptions.Value.Port}/speech?id={id}");
    }

    public async Task HandleSpeechProxyRequestAsync(HttpProxyHandler proxy)
    {
        var id = proxy.Query["id"];
        if (!Guid.TryParse(id, out var guid))
        {
            await proxy.WriteTextResponseAsync(HttpStatusCode.BadRequest, "id is required");
            return;
        }
        
        #warning Should be TryRemove
        if(!_pendingSpeechRequests.TryGetValue(guid, out var text))
        {
            await proxy.WriteTextResponseAsync(HttpStatusCode.BadRequest, $"No pending speech with id {guid}");
            return;
        }

        var querystring = new Dictionary<string, string>
        {
            ["text"] = text,
            ["voice"] = "-1",
            ["seed"] = "Naia",
            ["opus"] = "true",
            ["version"] = "v2"
        };
        var uriBuilder = new UriBuilder(new Uri(_httpClient.BaseAddress!, "/ai/generate-voice"))
        {
            Query = await new FormUrlEncodedContent(querystring).ReadAsStringAsync()
        };

        using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/webm"));
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        
        if (!response.IsSuccessStatusCode)
        {
            var reason = await response.Content.ReadAsStringAsync();
            // TODO: Logger
            Console.Error.WriteLine(reason);
            await proxy.WriteTextResponseAsync(HttpStatusCode.InternalServerError, "Unable to generate speech: " + reason);
        }

        // TODO: Optimize later
        var tmp = Path.GetTempFileName();
        var bytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(tmp, bytes);
        try
        {
            /* Option 1
            await using var webmStream = new MediaFoundationReader(tmp);
            using var outputStream = new MemoryStream();
            var targetWaveFormat = new WaveFormat(44100, 1);
            await using var waveWriter = new WaveFileWriter(outputStream, targetWaveFormat);
            var buffer = new byte[targetWaveFormat.AverageBytesPerSecond];
            int bytesRead;
            while ((bytesRead = webmStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                waveWriter.Write(buffer, 0, bytesRead);
            }
            waveWriter.Flush();
            outputStream.Seek(0, SeekOrigin.Begin);
            bytes = outputStream.ToArray();
            */
            /* Option 2
            using (var reader = new MediaFoundationReader(tmp))
            {
                var outFormat = new WaveFormat(44100, reader.WaveFormat.Channels);
                var ms = new MemoryStream();
                using (var resampler = new MediaFoundationResampler(reader, outFormat))
                {
                    // resampler.ResamplerQuality = 60;
                    WaveFileWriter.WriteWavFileToStream(ms, resampler);
                }
                bytes = ms.ToArray();
            }
            */
            /* Option 3 */
            await using var reader = new MediaFoundationReader(tmp);
            // var mediaType = MediaFoundationEncoder.SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, new WaveFormat(44100, 1), 0);
            // using var writer = new MediaFoundationEncoder(mediaType);
            var ms = new MemoryStream();
            MediaFoundationEncoder.EncodeToMp3(reader, ms, 16000);
            // writer.Encode(ms, reader, Guid.Empty);
            bytes = ms.ToArray();
        }
        finally
        {
            File.Delete(tmp);
        }

        await File.WriteAllBytesAsync(@"C:\Temp\tmp.wav", bytes);
        await proxy.WriteBytesResponseAsync(HttpStatusCode.OK, bytes, "audio/wav");
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private class NovelAIEventData
    {
        public required string token { get; init; }
        public bool final { get; init; }
        public int ptr { get; init; }
        public string? error { get; init; }
    }
}
