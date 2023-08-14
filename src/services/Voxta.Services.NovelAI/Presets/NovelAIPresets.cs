namespace Voxta.Services.NovelAI.Presets;

public static class NovelAIPresets
{
    public static NovelAIParameters DefaultForModel(string model)
    {
        return model switch
        {
            NovelAISettings.ClioV1 => ClioPresets.TalkerC(),
            NovelAISettings.KayraV1 => KayraPresets.FreshCoffee(),
            _ => throw new NotSupportedException("Model {model} not supported")
        };
    }
}
