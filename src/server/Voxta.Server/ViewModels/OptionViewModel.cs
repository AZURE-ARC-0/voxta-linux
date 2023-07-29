namespace Voxta.Server.ViewModels;

public class OptionViewModel
{
    public static OptionViewModel Create(string value) => new(value, value);

    public string Name { get; init; }
    public string Label { get; init; }

    public OptionViewModel(string name, string label)
    {
        Name = name;
        Label = label;
    }
}