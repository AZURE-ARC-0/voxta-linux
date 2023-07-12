namespace ChatMate.Services.OpenAI;

public static class OpenAISpecialTokens
{
    // ReSharper disable InconsistentNaming
    private const string IM_START = "<|im_start|>";
    private const string IM_END = "<|im_end|>";
    // ReSharper restore InconsistentNaming

    public static readonly Dictionary<string, int> SpecialTokens = new()
    {
        { IM_START, 100264},
        { IM_END, 100265},
    };
    public static readonly HashSet<string> Keys = new(SpecialTokens.Keys);
}