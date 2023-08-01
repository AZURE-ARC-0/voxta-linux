namespace Voxta.Services.NovelAI.Presets;

public static class NovelAIPresets
{
    public static NovelAIParameters DefaultForModel(string model)
    {
        return model switch
        {
            "clio-v1" => ClioPresets.TalkerC(),
            "kayra-v1" => KayraPresets.Carefree(),
            _ => throw new NotSupportedException("Model {model} not supported")
        };
    }
}
