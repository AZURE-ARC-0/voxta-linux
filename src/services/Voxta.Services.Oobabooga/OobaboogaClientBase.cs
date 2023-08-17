using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Repositories;
using Voxta.Shared.RemoteServicesUtils;

namespace Voxta.Services.Oobabooga;

public class OobaboogaClientBase : RemoteLLMServiceClientBase<OobaboogaSettings, OobaboogaParameters, OobaboogaRequestBody>
{
    protected override string ServiceName => OobaboogaConstants.ServiceName;
    
    protected override string GenerateRequestPath => "/api/v1/generate";
    
    protected OobaboogaClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
        : base(httpClientFactory, settingsRepository)
    {
    }

    protected OobaboogaRequestBody BuildRequestBody(string prompt, string[] stoppingStrings)
    {
        var body = CreateParameters();
        body.Prompt = prompt;
        body.StoppingStrings = stoppingStrings;
        return body;
    }

    protected async Task<string> SendCompletionRequest(OobaboogaRequestBody body, CancellationToken cancellationToken)
    {
        var json = await SendCompletionRequest<TextGenResponse>(body, cancellationToken);
        var text = json.results?[0].text ?? throw new OobaboogaException("Empty response");
        return text.TrimExcess();
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private class TextGenResponse
    {
        public List<TextGenResponseResult>? results { get; init; }
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private class TextGenResponseResult
    {
        public string? text { get; init; }
    }
}