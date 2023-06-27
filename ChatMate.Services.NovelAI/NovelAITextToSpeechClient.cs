using System.Net.Http.Headers;
using System.Security.Authentication;
using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.Extensions.Logging;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace ChatMate.Services.NovelAI;

public class NovelAITextToSpeechClient : ITextToSpeechService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NovelAITextToSpeechClient> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IPerformanceMetrics _performanceMetrics;

    static NovelAITextToSpeechClient()
    {
        MediaFoundationApi.Startup();
    }

    public NovelAITextToSpeechClient(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IPerformanceMetrics performanceMetrics)
    {
        _settingsRepository = settingsRepository;
        _performanceMetrics = performanceMetrics;
        _logger = loggerFactory.CreateLogger<NovelAITextToSpeechClient>();
        _httpClient = httpClientFactory.CreateClient("NovelAI");
        _httpClient.BaseAddress = new Uri("https://api.novelai.net");
    }

    public async Task GenerateSpeechAsync(SpeechRequest speechRequest, ISpeechTunnel tunnel, string extension)
    {
        var settings = await _settingsRepository.GetAsync<NovelAISettings>("NovelAI");
        if (string.IsNullOrEmpty(settings?.Token)) throw new AuthenticationException("NovelAI token is missing.");
        
        var querystring = new Dictionary<string, string>
        {
            ["text"] = speechRequest.Text,
            ["voice"] = "-1",
            ["seed"] = speechRequest.Voice,
            ["opus"] = "true",
            ["version"] = "v2"
        };
        var uriBuilder = new UriBuilder(new Uri(_httpClient.BaseAddress!, "/ai/generate-voice"))
        {
            Query = await new FormUrlEncodedContent(querystring).ReadAsStringAsync()
        };

        using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/webm"));
        if (string.IsNullOrEmpty(settings?.Token)) throw new AuthenticationException("NovelAI token is missing.");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",  Crypto.DecryptString(settings.Token));
        var ttsPerf = _performanceMetrics.Start("NovelAI.TextToSpeech");
        using var audioResponse = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        
        if (!audioResponse.IsSuccessStatusCode)
        {
            var reason = await audioResponse.Content.ReadAsStringAsync();
            _logger.LogError("Failed to generate speech: {Reason}", reason);
            await tunnel.ErrorAsync($"Unable to generate speech: {reason}");
            return;
        }

        // TODO: Optimize later (we're forced to use a temp file because of the MediaFoundationReader)
        string contentType;
        var tmp = Path.GetTempFileName();
        var bytes = await audioResponse.Content.ReadAsByteArrayAsync();
        ttsPerf.Done();
        var audioConvPerf = _performanceMetrics.Start("NovelAI.AudioConversion");
        await File.WriteAllBytesAsync(tmp, bytes);
        try
        {
            await using var reader = new MediaFoundationReader(tmp);
            var ms = new MemoryStream();
            switch (extension)
            {
                case "mp3":
                    contentType = "audio/mpeg";
                    MediaFoundationEncoder.EncodeToMp3(reader, ms, 192_000);
                    break;
                case "wav":
                    contentType = "audio/x-wav";
                    // var resampler = new MediaFoundationResampler(reader, 44100);
                    // var stereo = new MonoToStereoSampleProvider(resampler.ToSampleProvider());
                    // WaveFileWriter.WriteWavFileToStream(ms, stereo.ToWaveProvider16());
                    WaveFileWriter.WriteWavFileToStream(ms, reader);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected extension {extension}");
            }
            bytes = ms.ToArray();
        }
        finally
        {
            File.Delete(tmp);
        }
        audioConvPerf.Done();

        await tunnel.SendAsync(bytes, contentType);
    }
}