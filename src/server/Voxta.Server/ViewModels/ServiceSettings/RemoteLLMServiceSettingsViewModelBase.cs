using System.Diagnostics.CodeAnalysis;
using Voxta.Shared.LLMUtils;
using Voxta.Shared.RemoteServicesUtils;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public abstract class RemoteLLMServiceSettingsViewModelBase<TParameters> : LLMServiceSettingsViewModel
    where TParameters : class, new()
{
    public required string Uri { get; init; }
    public PromptFormats PromptFormat { get; init; }

    [SetsRequiredMembers]
    protected RemoteLLMServiceSettingsViewModelBase(RemoteLLMServiceSettingsBase<TParameters> source)
        : base(source, source.Parameters ?? new TParameters(), source.Parameters != null)
    {
        Uri = source.Uri;
        PromptFormat = source.PromptFormat;
    }
}