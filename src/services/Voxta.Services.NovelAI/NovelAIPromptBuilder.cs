using Voxta.Abstractions.Model;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.OpenSourceLargeLanguageModels;

public class NovelAIPromptBuilder : GenericPromptBuilder
{
    // https://docs.novelai.net/text/chatformat.html
    protected override string MakeSystemPrompt(IReadOnlyChatSessionData chatSessionData)
    {
        return base.MakeSystemPrompt(chatSessionData) + "\n***\n[ Style: chat ]";
    }
}