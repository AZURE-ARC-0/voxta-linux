namespace Voxta.Server.ViewModels;

public class OptionViewModel
{
    public static OptionViewModel Create(string value) => new(value, value);
    public static OptionViewModel Create(string name, string label) => new(name, label);

    public string Name { get; init; }
    public string Label { get; init; }

    public OptionViewModel(string name, string label)
    {
        Name = name;
        Label = label;
    }
}