namespace Voxta.Server.ViewModels.Playground;

public class TextToSpeechPlaygroundViewModel
{
    public List<OptionViewModel> Services { get; set; } = new();
    public List<OptionViewModel> Cultures { get; set; } = new();
}