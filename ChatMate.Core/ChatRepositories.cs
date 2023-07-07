using ChatMate.Abstractions.Repositories;

namespace ChatMate.Core;

public class ChatRepositories
{
    public IBotRepository Bots { get; }
    public IProfileRepository Profile { get; }
    public ISettingsRepository Settings { get; }

    public ChatRepositories(IBotRepository botRepository, IProfileRepository profileRepository, ISettingsRepository settingsRepository)
    {
        Bots = botRepository;
        Profile = profileRepository;
        Settings = settingsRepository;
    }
}