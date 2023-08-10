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
    private readonly IMemoryRepository _memoryRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ISettingsRepository _settingsRepository;

    public AutoRequestServicesStartupFilter(LiteDBMigrations migrations, ICharacterRepository charactersRepository, IProfileRepository profileRepository, ISettingsRepository settingsRepository, IMemoryRepository memoryRepository)
    {
        _migrations = migrations;
        _charactersRepository = charactersRepository;
        _profileRepository = profileRepository;
        _settingsRepository = settingsRepository;
        _memoryRepository = memoryRepository;
    }
    
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        Task.Run(async () =>
        {
            // Migrations
            await _migrations.MigrateAsync();
            
            // Default characters
            await _charactersRepository.SaveCharacterAsync(CatherineCharacter.Create());
            await _memoryRepository.SaveBookAsync(CatherineCharacter.CreateBook());
            
            await _charactersRepository.SaveCharacterAsync(GeorgeCharacter.Create());
            await _memoryRepository.SaveBookAsync(GeorgeCharacter.CreateBook());

            // Default profile
            var defaultProfile = ProfileUtils.CreateDefaultProfile();
            var profile = await _profileRepository.GetProfileAsync(CancellationToken.None);
            if (profile == null)
            {
                profile = defaultProfile;
                await _profileRepository.SaveProfileAsync(profile);
            }
            else
            {
                var modified = false;
                modified |= profile.TextGen.SyncWithTemplate(defaultProfile.TextGen);
                modified |= profile.SpeechToText.SyncWithTemplate(defaultProfile.SpeechToText);
                modified |= profile.TextToSpeech.SyncWithTemplate(defaultProfile.TextToSpeech);
                modified |= profile.ActionInference.SyncWithTemplate(defaultProfile.ActionInference);
                if (modified)
                {
                    await _profileRepository.SaveProfileAsync(profile);
                }
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
