using Voxta.Abstractions.Repositories;
using Voxta.Characters.Samples;
using Voxta.Data.LiteDB;
using Voxta.Host.AspNetCore.WebSockets.Utils;

namespace Voxta.Server.Filters;

public class AutoRequestServicesStartupFilter : IStartupFilter
{
    private readonly LiteDBMigrations _migrations;
    private readonly ICharacterRepository _charactersRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly IProfileRepository _profileRepository;

    public AutoRequestServicesStartupFilter(LiteDBMigrations migrations, ICharacterRepository charactersRepository, IProfileRepository profileRepository, IMemoryRepository memoryRepository)
    {
        _migrations = migrations;
        _charactersRepository = charactersRepository;
        _profileRepository = profileRepository;
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
                modified |= profile.Summarization.SyncWithTemplate(defaultProfile.Summarization);
                if (modified)
                {
                    await _profileRepository.SaveProfileAsync(profile);
                }
            }
        }).Wait();
        
        return next;
    }
}
