﻿using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Shared.LLMUtils;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public abstract class LLMServiceSettingsViewModel : ServiceSettingsWithParametersViewModel
{
    public required int MaxContextTokens { get; init; }
    public required int MaxMemoryTokens { get; init; }
    public required int SummarizationTriggerTokens { get; init; }
    public required int SummarizationDigestTokens { get; init; }
    public required int SummaryMaxTokens { get; init; }

    protected LLMServiceSettingsViewModel()
    {
    }
    
    [SetsRequiredMembers]
    protected LLMServiceSettingsViewModel(ConfiguredService service, LLMSettingsBase source, object parameters, bool useDefaults)
        : base(service, source, parameters, useDefaults)
    {
        MaxContextTokens = source.MaxContextTokens;
        MaxMemoryTokens = source.MaxMemoryTokens;
        SummarizationTriggerTokens = source.SummarizationTriggerTokens;
        SummarizationDigestTokens = source.SummarizationDigestTokens;
        SummaryMaxTokens = source.SummaryMaxTokens;
    }
}
