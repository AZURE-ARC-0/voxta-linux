namespace Voxta.Server.ViewModels;

public class OptionViewModel
{
    public static OptionViewModel Create(string value) => new() { Name = value, Label = value };
    
    public required string Name { get; init; }
    public required string Label { get; init; }
}