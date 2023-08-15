using Voxta.Shared.LLMUtils;

namespace Voxta.Server.ViewModels.ServiceSettings;

public static class PromptFormatsViewModel
{
    public static readonly OptionViewModel[] Values = new OptionViewModel[]
    {
        new(PromptFormats.Generic.ToString(), "Generic"),
        new(PromptFormats.Alpaca.ToString(), "Alpaca"),
        new(PromptFormats.Llama2.ToString(), "Llama2"),
    };
}