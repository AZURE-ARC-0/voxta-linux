using System.ComponentModel.DataAnnotations;
using Voxta.Abstractions.Repositories;

namespace Voxta.Services.WindowsSpeech;

[Serializable]
public class WindowsSpeechSettings : SettingsBase
{
    [Range(0, 1)]
    public double MinimumConfidence { get; set; } = 0.25;
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
