using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Repositories;
using Voxta.Common;
using Voxta.Shared.RemoteServicesUtils;

namespace Voxta.Services.KoboldAI;

public class KoboldAIClientBase : RemoteLLMServiceClientBase<KoboldAISettings, KoboldAIParameters, KoboldAIRequestBody>
{
    protected override string ServiceName => KoboldAIConstants.ServiceName;
    protected override string GenerateRequestPath => "/api/extra/generate/stream";

    protected KoboldAIClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
        : base(httpClientFactory, settingsRepository)

    {
        httpClientFactory.CreateClient(KoboldAIConstants.ServiceName);
    }

    protected KoboldAIRequestBody BuildRequestBody(string prompt, string[] stoppingStrings)
    {
        var body = CreateParameters();
        body.Prompt = prompt;
        body.StopSequence = stoppingStrings;
        // TODO: We want to add logit bias here to avoid [, ( and OOC from being generated.
        return body;
    }

    protected async Task<string> SendCompletionRequest(KoboldAIRequestBody body, CancellationToken cancellationToken)
    {
        return await SendStreamingCompletionRequest<TextGenEventData>(body, cancellationToken);
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class TextGenEventData : IEventStreamData
    {
        public required string token { get; init; }
        public bool final { get; init; }
        public int ptr { get; init; }
        public string? error { get; init; }
        public string GetToken() => token;
    }
}