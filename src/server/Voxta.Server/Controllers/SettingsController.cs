using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Services;
using Voxta.Host.AspNetCore.WebSockets.Utils;
using Voxta.Server.ViewModels.Settings;

namespace Voxta.Server.Controllers;

[Controller]
public class SettingsController : Controller
{
    private readonly IProfileRepository _profileRepository;
    private readonly IServicesRepository _servicesRepository;
    private readonly IServiceDefinitionsRegistry _serviceDefinitionsRegistry;

    public SettingsController(IProfileRepository profileRepository, IServicesRepository servicesRepository, IServiceDefinitionsRegistry serviceDefinitionsRegistry)
    {
        _profileRepository = profileRepository;
        _servicesRepository = servicesRepository;
        _serviceDefinitionsRegistry = serviceDefinitionsRegistry;
    }
    
    [HttpGet("/settings")]
    [SuppressMessage("ReSharper", "RawStringCanBeSimplified")]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        if (profile == null)
        {
            return View(new SettingsViewModel
            {
                Profile = null,
                Services = Array.Empty<ConfiguredServiceViewModel>(),
                ServiceTypes = Array.Empty<ServiceLinksViewModel>(),
            });
        }

        var services = await _servicesRepository.GetServicesAsync(cancellationToken);
        var linkVms = services
            .Select(x => new ServiceLinkViewModel
            {
                Enabled = x.Enabled,
                ServiceName = x.ServiceName,
                ServiceId = x.Id,
                ServiceDefinition = _serviceDefinitionsRegistry.Get(x.ServiceName)
            })
            .ToList();
        
        var serviceTypes = new ServiceLinksViewModel[]
        {
            new()
            {
                Type = "stt",
                Title = "Speech To Text Services",
                ServiceLinks = GetServiceLinks(profile.SpeechToText, linkVms, s => s.ServiceDefinition.STT),
                Help = """
                       <p>Allows you to speak using your voice.</p>
                       <p>Azure Speech Service is highly recommended because it supports punctuation and works extremely well. It is also free up to a point. Otherwise, Vosk is free, fast and runs locally, despite being inferior to Azure.</p>
                       """
            },
            new()
            {
                Type = "textgen",
                Title = "Text Generation Services",
                ServiceLinks = GetServiceLinks(profile.TextGen, linkVms, s => s.ServiceDefinition.TextGen),
                Help = """
                       <p>The brain of the AI, this is what writes the replies.</p>
                       <p>Some recommendations:</p>
                       <ul>
                           <li>
                               <p><b>Easy, fast and unrestricted: NovelAI</b></p>
                               <p>For fast and natural speech with minimal setup, use NovelAI for both Text Gen and TTS.</p>
                           </li>
                           <li>
                               <p><b>Easy, fast, coherent but restricted: OpenAI</b></p>
                               <p>For a helpful AI that knows things and can really help with questions, use OpenAI for Text Gen. GPT-4 is very good, but also much slower and more expensive.</p>
                           </li>
                           <li>
                               <p><b>Unrestricted, but slower, requires a GPU and some efforts to setup: Text Generation Web UI or KoboldAI</b></p>
                               <p>For unrestricted chat, use Text Generation Web UI or KoboldAI for TextGen. You need to host them yourself or on RunPod.io (or another host), and they are more complicated to setup, but they won't cost you anything and they can run completely locally.</p>
                           </li>
                       </ul>
                       """
            },
            new()
            {
                Type = "tts",
                Title = "Text To Speech Services",
                ServiceLinks = GetServiceLinks(profile.TextToSpeech, linkVms, s => s.ServiceDefinition.TTS),
                Help = """
                   <p>This is what gives a voice to the character.</p>
                   <p>Some recommendations:</p>
                   <ul>
                       <li>
                           <p><b>The best available option for english: NovelAI</b></p>
                           <p>For fast and natural speech with minimal setup, use NovelAI for both Text Gen and TTS.</p>
                       </li>
                       <li>
                           <p><b>Excellent quality, slightly slower and expensive but supports other languages: ElevenLabs</b></p>
                           <p>ElevenLabs has an amazing quality with decent speed, but higher usage can get expensive.</p>
                       </li>
                       <li>
                           <p><b>Fair quality: Azure Speech Service</b></p>
                           <p>Azure Speech Service is fair, but not great. Still a good, free option for low use.</p>
                       </li>
                   </ul>
                   """
            },
            new()
            {
                Type = "action_inference",
                Title = "Action Inference Services",
                ServiceLinks = GetServiceLinks(profile.ActionInference, linkVms, s => s.ServiceDefinition.ActionInference),
                Help = """
                       <p>This is what allows the AI to do things with their avatar in virtual reality.</p>
                       <p>You should use OpenAI, even for NSFW, unless you want to experiment with other LLMs. Keep in mind, the LLM must be good to do action inference correctly.</p>
                       """
            },
            new()
            {
                Type = "summarization",
                Title = "Summarization Services",
                ServiceLinks = GetServiceLinks(profile.Summarization, linkVms, s => s.ServiceDefinition.Summarization),
                Help = """
                   <p>This is what allows longer conversations by compression memories.</p>
                   <p>Summarization is used to replace long chat history by summaries. This results in a stronger character adherence and faster inference.</p>
                   """
            }
        };
        var servicesVMs = services
            .OrderBy(x => x.Label)
            .ThenBy(x => x.ServiceName)
            .Select(x => new ConfiguredServiceViewModel
            {
                Service = x,
                Definition = _serviceDefinitionsRegistry.Get(x.ServiceName),
            })
            .ToArray();
        var vm = new SettingsViewModel
        {
            Profile = await _profileRepository.GetProfileAsync(cancellationToken),
            Services = servicesVMs,
            ServiceTypes = serviceTypes
        };
        return View(vm);
    }

    private static List<ServiceLinkViewModel> GetServiceLinks(ServicesList services, IReadOnlyCollection<ServiceLinkViewModel> linkVms, Func<ServiceLinkViewModel, bool> filter)
    {
        var filtered = linkVms.Where(filter).ToList();
        var result = services.Services
            .Select(x => linkVms.FirstOrDefault(l => l.ServiceId == x.ServiceId) ?? filtered.First(l => l.ServiceName == x.ServiceName))
            .ToList();
        return result;
    }

    [HttpPost("/settings/reorder")]
    public async Task<IActionResult> ReorderServices([FromForm] string serviceType, [FromForm] string orderedServices, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetRequiredProfileAsync(CancellationToken.None);
        switch (serviceType)
        {
            case "stt":
                profile.SpeechToText = Reorder(profile.SpeechToText, orderedServices);
                break;
            case "tts":
                profile.TextToSpeech = Reorder(profile.TextToSpeech, orderedServices);
                break;
            case "action_inference":
                profile.ActionInference = Reorder(profile.ActionInference, orderedServices);
                break;
            case "textgen":
                profile.TextGen = Reorder(profile.TextGen, orderedServices);
                break;
            case "summarization":
                profile.Summarization = Reorder(profile.Summarization, orderedServices);
                break;
            default:
                throw new NotSupportedException($"Cannot reorder {serviceType}");
        }

        await _profileRepository.SaveProfileAsync(profile);
        return RedirectToAction("Settings");
    }

    private static ServicesList Reorder(ServicesList services, string orderedServices)
    {
        services.Services = orderedServices
            .Split(',')
            .Select(x =>
            {
                var values = x.Split(':');
                return new ServiceLink
                {
                    ServiceName = values[0],
                    ServiceId = Guid.TryParse(values[1], out var id) && id != Guid.Empty ? id : null,
                };
            })
            .ToArray();
        return services;
    }

    [HttpGet("/settings/add")]
    public async Task<IActionResult> AddService([FromServices] IServiceDefinitionsRegistry serviceDefinitionsRegistry, CancellationToken cancellationToken)
    {
        var configured = await _servicesRepository.GetServicesAsync(cancellationToken);
        var services = serviceDefinitionsRegistry.List()
            .OrderBy(s => s.ServiceName)
            .Select(s => new AddServiceViewModel.ServiceEntryViewModel
            {
                Definition = s,
                Occurrences = configured.Count(x => x.ServiceName == s.ServiceName),
                EnabledOccurrences = configured.Count(x => x.Enabled && x.ServiceName == s.ServiceName),
            })
            .ToArray();
        return View(new AddServiceViewModel
        {
            Services = services
        });
    }

    [HttpGet("/settings/profile")]
    public async Task<IActionResult> ProfileSettings(CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        var exists = profile != null;
        profile ??= ProfileUtils.CreateDefaultProfile();
        var vm = new ProfileViewModel
        {
            Name = profile.Name,
            Description = profile.Description ?? "",
            PauseSpeechRecognitionDuringPlayback = profile.PauseSpeechRecognitionDuringPlayback,
            IsAdult = exists,
            AgreesToTerms = exists,
        };
        return View(vm);
    }
    
    [HttpPost("/settings/profile")]
    public async Task<IActionResult> PostProfileSettings([FromForm] ProfileViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("ProfileSettings", value);
        }
        
        var profile = await _profileRepository.GetProfileAsync(CancellationToken.None) ?? ProfileUtils.CreateDefaultProfile();

        profile.Name = value.Name;
        profile.Description = value.Description;
        profile.PauseSpeechRecognitionDuringPlayback = value.PauseSpeechRecognitionDuringPlayback;

        await _profileRepository.SaveProfileAsync(profile);
        
        return RedirectToAction("Settings");
    }
}