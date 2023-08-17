using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;
using Voxta.Common;
using Voxta.Services.NovelAI.Presets;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.NovelAI;

public class NovelAIClientBase : LLMServiceClientBase<NovelAISettings, NovelAIParameters, NovelAIRequestBodyParameters>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public override string ServiceName => NovelAIConstants.ServiceName;

    protected override ITokenizer Tokenizer { get; } = new NovelAITokenizer();
    
    private readonly HttpClient _httpClient;
    private readonly ILocalEncryptionProvider _encryptionProvider;
    private string _model = NovelAISettings.KayraV1;

    protected NovelAIClientBase(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, ILocalEncryptionProvider encryptionProvider)
        : base(settingsRepository)
    {
        _encryptionProvider = encryptionProvider;
        _httpClient = httpClientFactory.CreateClient(NovelAIConstants.ServiceName);
    }
    
    protected override async Task<bool> TryInitializeAsync(NovelAISettings settings, string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;

        return TryInitializeSync(settings, prerequisites, culture, dry);
    }

    private bool TryInitializeSync(NovelAISettings settings, string[] prerequisites, string culture, bool dry)
    {
        if (string.IsNullOrEmpty(settings.Token)) return false;
        if (!culture.StartsWith("en") && !culture.StartsWith("jp")) return false;
        if (!ValidateSettings(settings)) return true;
        if (dry) return true;

        _httpClient.BaseAddress = new Uri("https://api.novelai.net");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _encryptionProvider.Decrypt(settings.Token));
        _model = settings.Model;
        return true;
    }
    
    protected override NovelAIParameters CreateDefaultParameters(NovelAISettings settings)
    {
        return NovelAIPresets.DefaultForModel(settings.Model);
    }

    protected virtual bool ValidateSettings(NovelAISettings settings)
    {
        return true;
    }

    protected NovelAIRequestBody BuildRequestBody(string prompt, string prefix)
    {
        var parameters = CreateParameters();
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

        var body = new NovelAIRequestBody
        {
            Model = _model,
            Input = prompt,
            Parameters = parameters
        };
        return body;
    }

    protected async ValueTask<string> SendCompletionRequest(NovelAIRequestBody body, CancellationToken cancellationToken)
    {
        var bodyContent = new StringContent(JsonSerializer.Serialize(body, JsonSerializerOptions), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/ai/generate-stream");
        request.Content = bodyContent;
        request.ConfigureEventStream();

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new NovelAIException(await response.Content.ReadAsStringAsync(cancellationToken));
        var text = await response.ReadEventStream<NovelAIEventData>(cancellationToken);
        return text.TrimExcess();
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
}