namespace Voxta.Server.ViewModels;

public class NovelAISettingsViewModel : ServiceSettingsViewModel
{
    public required string Token { get; set; }
    public required string Model { get; set; }
}


public class OpenAISettingsViewModel : ServiceSettingsViewModel
{
    public required string ApiKey { get; set; }
    public required string Model { get; set; }
}


public class KoboldAISettingsViewModel : ServiceSettingsViewModel
{
    public required string Uri { get; set; }
}

public class OobaboogaSettingsViewModel : ServiceSettingsViewModel
{
    public required string Uri { get; set; }
}
