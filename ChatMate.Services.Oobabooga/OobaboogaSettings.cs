using ChatMate.Abstractions.Repositories;

namespace ChatMate.Services.Oobabooga;

[Serializable]
public class OobaboogaSettings : SettingsBase
{
    public string? Uri { get; set; }
}