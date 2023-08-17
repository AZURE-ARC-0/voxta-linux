using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Characters.Samples;
using Voxta.Data.LiteDB;

namespace Voxta.Server.Filters;

public class AutoRequestServicesStartupFilter : IStartupFilter
{
    private readonly LiteDBMigrations _migrations;
    private readonly ICharacterRepository _charactersRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IServicesRepository _servicesRepository;
    private readonly IServiceDefinitionsRegistry _serviceDefinitions;

    public AutoRequestServicesStartupFilter(LiteDBMigrations migrations, ICharacterRepository charactersRepository, IMemoryRepository memoryRepository, IProfileRepository profileRepository, IServicesRepository servicesRepository, IServiceDefinitionsRegistry serviceDefinitions)
    {
        _migrations = migrations;
        _charactersRepository = charactersRepository;
        _memoryRepository = memoryRepository;
        _profileRepository = profileRepository;
        _servicesRepository = servicesRepository;
        _serviceDefinitions = serviceDefinitions;
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
            
            // Fix Services
            var profile = await _profileRepository.GetProfileAsync(CancellationToken.None);
            if (profile != null)
            {
                var services = await _servicesRepository.GetServicesAsync();
                CleanupProfile(profile.SpeechToText, services, d => d.STT);
                CleanupProfile(profile.TextGen, services, d => d.TextGen);
                CleanupProfile(profile.TextToSpeech, services, d => d.TTS);
                CleanupProfile(profile.ActionInference, services, d => d.ActionInference);
                CleanupProfile(profile.Summarization, services, d => d.Summarization);
                profile.LastConnected = DateTimeOffset.UtcNow;
                await _profileRepository.SaveProfileAsync(profile);
            }
        }).Wait();
        
        return next;
    }

    private void CleanupProfile(ServicesList servicesList, ConfiguredService[] services, Func<ServiceDefinition, ServiceDefinitionCategoryScore> getScore)
    {
        // If any services are missing from servicesList, add them
        var links = new List<ServiceLink>(servicesList.Services);
        var added = false;
        foreach (var service in services)
        {
            var definition = _serviceDefinitions.Get(service.ServiceName);
            if (definition.SettingsType == null)
            {
                // Source was deleted
                links.RemoveAll(x => x.ServiceName == service.ServiceName);
            }
            else
            {
                // Source was missing
                if (links.All(x => x.ServiceId != service.Id))
                {
                    links.Add(new ServiceLink
                    {
                        ServiceName = service.ServiceName,
                        ServiceId = service.Id,
                    });
                    added = true;
                }
            }
        }
        
        // Invalid entry
        links.RemoveAll(l => !getScore(_serviceDefinitions.Get(l.ServiceName)).IsSupported());
        
        // Reorder if new services were added
        servicesList.Services = added
            ? links.OrderByDescending(x => getScore(_serviceDefinitions.Get(x.ServiceName))).ToArray()
            : links.ToArray();
    }
}
