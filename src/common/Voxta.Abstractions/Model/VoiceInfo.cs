namespace Voxta.Abstractions.Model;

public class VoiceInfo
{
    public static VoiceInfo[] DefaultVoices => new VoiceInfo[]
    {
        new() { Id = "", Label = "Unspecified" },
        new() { Id = SpecialVoices.Male, Label = "Male" },
        new() { Id = SpecialVoices.Female, Label = "Female" },
    };

    public required string Id { get; init; }
    public required string Label { get; init; }
}