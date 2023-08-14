using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.System;
using Voxta.Services.NovelAI;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class NovelAISettingsViewModel : LLMServiceSettingsViewModel
{
    public required string Token { get; set; }
    public required string Model { get; set; }
    public required string ThinkingSpeech { get; set; }

    [SetsRequiredMembers]
    public NovelAISettingsViewModel(NovelAISettings source, ILocalEncryptionProvider encryptionProvider)
        : base(source, source.Parameters ?? new NovelAIParameters(), source.Parameters != null)
    {
        Token = encryptionProvider.SafeDecrypt(source.Token);
        Model = source.Model;
        ThinkingSpeech = string.Join('\n', source.ThinkingSpeech);
    }

    public NovelAISettings ToSettings(ILocalEncryptionProvider encryptionProvider)
    {
        return new NovelAISettings
        {
            Enabled = Enabled,
            Token = string.IsNullOrEmpty(Token) ? "" : encryptionProvider.Encrypt(Token.TrimCopyPasteArtefacts()),
            Model = Model.TrimCopyPasteArtefacts(),
            MaxContextTokens = MaxContextTokens,
            MaxMemoryTokens = MaxMemoryTokens,
            SummaryMaxTokens = SummaryMaxTokens,
            SummarizationDigestTokens = SummarizationDigestTokens,
            SummarizationTriggerTokens = SummarizationTriggerTokens,
            Parameters = GetParameters<NovelAIParameters>(),
            ThinkingSpeech = ThinkingSpeech.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        };
    }
}