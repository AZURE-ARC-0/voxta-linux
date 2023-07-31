using Voxta.Abstractions.Repositories;
using Voxta.Characters.Samples;
using Voxta.Data.LiteDB;
using Voxta.Host.AspNetCore.WebSockets.Utils;
using Voxta.Services.Vosk;
#if(WINDOWS)
using Voxta.Services.WindowsSpeech;
#endif

namespace Voxta.Server.Filters;

public class AutoRequestServicesStartupFilter : IStartupFilter
{
    private readonly LiteDBMigrations _migrations;
    private readonly ICharacterRepository _charactersRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ISettingsRepository _settingsRepository;

    public AutoRequestServicesStartupFilter(LiteDBMigrations migrations, ICharacterRepository charactersRepository, IProfileRepository profileRepository, ISettingsRepository settingsRepository)
    {
        _migrations = migrations;
        _charactersRepository = charactersRepository;
        _profileRepository = profileRepository;
        _settingsRepository = settingsRepository;
    }
    
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        Task.Run(async () =>
        {
            // Migrations
            await _migrations.MigrateAsync();
            
            // Default characters
            await _charactersRepository.SaveCharacterAsync(CatherineCharacter.Create());
            await _charactersRepository.SaveCharacterAsync(GeorgeCharacter.Create());

            // Default profile
            var profile = await _profileRepository.GetProfileAsync(CancellationToken.None);
            if (profile == null)
            {
                profile = ProfileUtils.CreateDefaultProfile();
                await _profileRepository.SaveProfileAsync(profile);
            }
            
            // Default services
            #if(WINDOWS)
            var windowsspeech = await _settingsRepository.GetAsync<WindowsSpeechSettings>(CancellationToken.None);
            if (windowsspeech == null)
            {
                windowsspeech = new WindowsSpeechSettings();
                await _settingsRepository.SaveAsync(windowsspeech);
            }
            #endif
            
            var vosk = await _settingsRepository.GetAsync<VoskSettings>(CancellationToken.None);
            if (vosk == null)
            {
                vosk = new VoskSettings();
                await _settingsRepository.SaveAsync(vosk);
            }
        }).Wait();
        
        return next;
    }
}
