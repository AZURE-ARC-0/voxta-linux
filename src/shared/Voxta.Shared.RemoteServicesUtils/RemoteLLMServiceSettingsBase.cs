using Voxta.Shared.LLMUtils;

namespace Voxta.Shared.RemoteServicesUtils;

[Serializable]
public class RemoteLLMServiceSettingsBase<TParameters> : LLMSettingsBase<TParameters>
{
    public required string Uri { get; set; }
    public PromptFormats PromptFormat { get; set; } = PromptFormats.Generic;
}
