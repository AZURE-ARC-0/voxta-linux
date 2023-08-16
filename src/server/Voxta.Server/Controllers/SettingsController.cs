﻿using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Services;
using Voxta.Host.AspNetCore.WebSockets.Utils;
using Voxta.Server.ViewModels.Settings;
#if(WINDOWS)
#endif

namespace Voxta.Server.Controllers;

[Controller]
public class SettingsController : Controller
{
    private readonly IProfileRepository _profileRepository;
    private readonly IServicesRepository _servicesRepository;
    private readonly IServiceHelpRegistry _serviceHelpRegistry;
    private readonly DiagnosticsUtil _diagnosticsUtil;

    public SettingsController(IProfileRepository profileRepository, DiagnosticsUtil diagnosticsUtil, IServicesRepository servicesRepository, IServiceHelpRegistry serviceHelpRegistry)
    {
        _profileRepository = profileRepository;
        _diagnosticsUtil = diagnosticsUtil;
        _servicesRepository = servicesRepository;
        _serviceHelpRegistry = serviceHelpRegistry;
    }
    
    [HttpGet("/settings")]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var vm = await GetSettingsViewModel(() => _diagnosticsUtil.GetAllServicesAsync(cancellationToken), cancellationToken);
        return View(vm);
    }

    [HttpPost("/settings/test")]
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
    public async Task<IActionResult> TestAllSettings([FromForm] bool test, CancellationToken cancellationToken)
    {
        if (!test) throw new InvalidOperationException("Unexpected settings test without test flag.");
        var vm = await GetSettingsViewModel(() => _diagnosticsUtil.TestAllServicesAsync(cancellationToken), cancellationToken);
        return View("Settings", vm);
    }

    [SuppressMessage("ReSharper", "RawStringCanBeSimplified")]
    private async Task<SettingsViewModel> GetSettingsViewModel(Func<Task<DiagnosticsResult>> getServiceTypes, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        if (profile == null)
        {
            return new SettingsViewModel
            {
                Profile = null,
                Services = Array.Empty<ConfiguredServiceViewModel>(),
                ServiceTypes = Array.Empty<ServiceAssignationTypesViewModel>(),
            };
        }
        var services = await _servicesRepository.GetServicesAsync(cancellationToken);
        var serviceTypes = await getServiceTypes();
        var servicesVMs = services
            .OrderBy(x => x.Label)
            .ThenBy(x => x.ServiceName)
            .Select(x => new ConfiguredServiceViewModel
            {
                Service = x,
                Help = _serviceHelpRegistry.Get(x.ServiceName),
            })
            .ToArray();
        var vm = new SettingsViewModel
        {
            Profile = profile,
            Services = servicesVMs,
            ServiceTypes = new ServiceAssignationTypesViewModel[]
            {
                new()
                {
                    Name = "profile",
                    Title = "Profile",
                    Services = new[] { serviceTypes.Profile },
                    Help = """
                        <p>Your profile defines how the AI will see you and call you.</p>
                        """
                },
                new()
                {
                    Name = "stt",
                    Title = "Speech To Text Services",
                    Services = serviceTypes.SpeechToTextServices,
                    Help = """
                        <p>Azure Speech Service is highly recommended because it supports punctuation and works extremely well. It is also free up to a point. Otherwise, Vosk is free, fast and runs locally, despite being inferior to Azure.</p>
                        """
                },
                new()
                {
                    Name = "textgen",
                    Title = "Text Generation Services",
                    Services = serviceTypes.TextGenServices,
                    Help = """
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
                    Name = "tts",
                    Title = "Text To Speech Services",
                    Services = serviceTypes.TextToSpeechServices,
                    Help = """
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
                    Name = "action_inference",
                    Title = "Action Inference Services",
                    Services = serviceTypes.ActionInferenceServices,
                    Help = """
                        <p>You should use OpenAI, even for NSFW, unless you want to experiment with other LLMs. Keep in mind, the LLM must be good to do action inference correctly.</p>
                        """
                },
                new()
                {
                    Name = "summarization",
                    Title = "Summarization Services",
                    Services = serviceTypes.SummarizationServices,
                    Help = """
                           <p>Summarization is used to replace long chat history by summaries. This results in a stronger character adherence and faster inference.</p>
                           """
                },
            }
        };
        return vm;
    }

    [HttpGet("/settings/add")]
    public async Task<IActionResult> AddService([FromServices] IServiceHelpRegistry serviceHelpRegistry, CancellationToken cancellationToken)
    {
        var configured = await _servicesRepository.GetServicesAsync(cancellationToken);
        var services = serviceHelpRegistry.List()
            .OrderBy(s => s.ServiceName)
            .Select(s => new AddServiceViewModel.ServiceEntryViewModel
            {
                Help = s,
                Occurrences = configured.Count(x => x.ServiceName == s.ServiceName),
                EnabledOccurrences = configured.Count(x => x.Enabled && x.ServiceName == s.ServiceName),
            })
            .ToArray();
        return View(new AddServiceViewModel
        {
            Services = services
        });
    }

    [HttpPost("/settings/reorder")]
    public async Task<ActionResult> Reorder([FromForm] string type, [FromForm] string name, [FromForm] string direction)
    {
        var profile = await _profileRepository.GetRequiredProfileAsync(CancellationToken.None);
        switch (type)
        {
            case "stt":
                profile.SpeechToText = Reorder(profile.SpeechToText, name, direction);
                break;
            case "tts":
                profile.TextToSpeech = Reorder(profile.TextToSpeech, name, direction);
                break;
            case "action_inference":
                profile.ActionInference = Reorder(profile.ActionInference, name, direction);
                break;
            case "textgen":
                profile.TextGen = Reorder(profile.TextGen, name, direction);
                break;
            case "summarization":
                profile.Summarization = Reorder(profile.Summarization, name, direction);
                break;
            default:
                throw new NotSupportedException($"Cannot reorder {type}");
        }

        await _profileRepository.SaveProfileAsync(profile);
        return RedirectToAction("Settings");
    }

    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
    private static ServicesList Reorder(ServicesList services, string name, string direction)
    {
        if (direction != "up") throw new NotSupportedException("Only up is supported");
        var index = Array.IndexOf(services.Services, name);
        if (index == 0) return services;
        var list = new List<ServiceLink>(services.Services);
        var item = services.Services[index];
        if (index == -1)
        {
            list.Add(item);
        }
        else
        {
            list.RemoveAt(index);
            list.Insert(index - 1, item);
        }
        services.Services = list.ToArray();
        return services;
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