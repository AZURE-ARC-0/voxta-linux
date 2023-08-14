using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Repositories;
using Voxta.Common;
using Voxta.Shared.RemoteServicesUtils;

namespace Voxta.Services.TextGenerationInference;

public class TextGenerationInferenceClientBase : RemoteLLMServiceClientBase<TextGenerationInferenceSettings, TextGenerationInferenceParameters, TextGenerationInferenceParametersBody>
{
    protected override string GenerateRequestPath => "/generate_stream";
    
    protected TextGenerationInferenceClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
        : base(TextGenerationInferenceConstants.ServiceName, httpClientFactory, settingsRepository)
    {
    }

    protected TextGenerationInferenceRequestBody BuildRequestBody(string prompt, string[] stoppingStrings)
    {
        var parameters = CreateParameters();
        parameters.Stop = stoppingStrings;
        return new TextGenerationInferenceRequestBody
        {
            Parameters = parameters,
            Inputs = prompt,
        };
    }

    protected async Task<string> SendCompletionRequest(TextGenerationInferenceRequestBody body, CancellationToken cancellationToken)
    {
        return await SendStreamingCompletionRequest<TextGenEventData>(body, cancellationToken);
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class TextGenEventData : IEventStreamData
    {
        public required TextGenEventDataToken token { get; init; }
        public string? generated_text { get; init; }
        public string? details { get; init; }
        public string GetToken() => token.GetToken();
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    private class TextGenEventDataToken : IEventStreamData
    {
        public int id { get; init; }
        public required string text { get; init; }
        public double? logprob { get; init; }
        public bool special { get; init; }
        public string GetToken() => special ? "" : text;
    }
}
