namespace Voxta.Server.ViewModels.Playground;

public class TextToSpeechPlaygroundViewModel
{
    public List<OptionViewModel> Services { get; set; } = new();
    public List<OptionViewModel> Cultures { get; set; } = new();
}

public class TextGenPlaygroundViewModel
{
    public required List<OptionViewModel> Services { get; init; } = new();
    public string? Service { get; set; }
    public required List<OptionViewModel> Characters { get; init; } = new();
    public string? Character { get; init; }
    public string Prompt { get; set; } = "";
    public string? Response { get; set; }
    public string? Culture { get; init; }
}