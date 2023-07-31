using Voxta.Abstractions.Repositories;

namespace Voxta.Services.WindowsSpeech;

[Serializable]
public class WindowsSpeechSettings : SettingsBase
{
    public string[] ThinkingSpeech { get; set; } = {
        "m",
        "uh",
        "..",
        "...",
        "mmh",
        "hum",
        "huh",
        "!!",
        "??",
        "o",
    };
}
