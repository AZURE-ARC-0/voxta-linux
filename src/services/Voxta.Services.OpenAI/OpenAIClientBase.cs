﻿using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Voxta.Abstractions;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.OpenAI;

public abstract class OpenAIClientBase : LLMServiceClientBase<OpenAISettings, OpenAIParameters, OpenAIRequestBody>
{
    protected override ITokenizer Tokenizer => _tokenizer ?? throw new NullReferenceException("Tokenizer was not initialized.");
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    protected override string ServiceName => OpenAIConstants.ServiceName;
    
    private readonly ILocalEncryptionProvider _encryptionProvider;
    private readonly HttpClient _httpClient;
    private string _model = "gpt-3.5-turbo";
    private ITokenizer? _tokenizer;

    protected OpenAIClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ILocalEncryptionProvider encryptionProvider)
        : base(settingsRepository)
    {
        _encryptionProvider = encryptionProvider;
        _httpClient = httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}");
    }

    protected override async Task<bool> TryInitializeAsync(OpenAISettings settings, IPrerequisitesValidator prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;

        if (string.IsNullOrEmpty(settings.ApiKey)) return false;
        if (dry) return true;

        _tokenizer = await DeepDevTokenizer.GetSharedInstanceAsync();
        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _encryptionProvider.Decrypt(settings.ApiKey));
        _model = settings.Model;
        return true;
    }

    protected OpenAIRequestBody BuildRequestBody(IEnumerable<MessageData> messages)
    {
        var body = CreateParameters();
        body.Messages = messages.Select(m => new OpenAIMessage
        {
            role = m.Role switch
            {
                ChatMessageRole.System => "system",
                ChatMessageRole.Assistant => "assistant",
                ChatMessageRole.User => "user",
                _ => throw new ArgumentOutOfRangeException(null, $"Unknown role: {m.Role}")
            },
            content = m.Value
        }).ToList();
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
}