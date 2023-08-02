using Voxta.Abstractions.Model;
using Voxta.Abstractions.Tokenizers;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.OpenSourceLargeLanguageModels;

public class NovelAIPromptBuilder : GenericPromptBuilder
{
    public NovelAIPromptBuilder(ITokenizer tokenizer)
        : base(tokenizer)
    {
    }

    // https://docs.novelai.net/text/chatformat.html
    protected override string MakeSystemPrompt(IChatInferenceData chat)
    {
        return base.MakeSystemPrompt(chat) + "\n***\n[ Style: chat ]";
    }
}