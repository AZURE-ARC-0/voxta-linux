using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Voxta.Core;
using Voxta.Server.ViewModels;
using Voxta.Server.ViewModels.Playground;
using Voxta.Services.OpenAI;

namespace Voxta.Server.Controllers;

[Controller]
public class PlaygroundController : Controller
{
    private readonly IProfileRepository _profileRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IServiceObserver _serviceObserver;
    private readonly IServicesRepository _serviceRepository;

    public PlaygroundController(IProfileRepository profileRepository, ICharacterRepository characterRepository, IServiceObserver serviceObserver, IServicesRepository serviceRepository)
    {
        _profileRepository = profileRepository;
        _characterRepository = characterRepository;
        _serviceObserver = serviceObserver;
        _serviceRepository = serviceRepository;
    }
    
    [HttpGet("/playground")]
    public ActionResult Index()
    {
        return View();
    }

    [HttpGet("/playground/text-to-speech")]
    public async Task<ActionResult> TextToSpeech(CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        if (profile == null) return RedirectToAction("Settings", "Settings");
        return View(new TextToSpeechPlaygroundViewModel
        {
            Services = profile.TextToSpeech.Services.Select(l => OptionViewModel.Create($"{l.ServiceName}/{l.ServiceId}", l.ServiceName)).ToList(),
            Cultures = CultureUtils.Bcp47LanguageTags.Select(x => new OptionViewModel(x.Name, x.Label)).ToList(),
        });
    }
    
    [HttpGet("/playground/text-gen")]
    public async Task<ActionResult> TextGen(CancellationToken cancellationToken, [FromQuery] Guid? characterId, [FromQuery] string? service, [FromQuery] string? observerKey)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        if (profile == null) return RedirectToAction("Settings", "Settings");
        var character = characterId != null ? await _characterRepository.GetCharacterAsync(characterId.Value, cancellationToken) : null;
        var vm = await GetTextGenViewModel(profile, service, cancellationToken, character);
        if (!string.IsNullOrEmpty(observerKey))
            vm.Prompt = _serviceObserver.GetRecord(observerKey)?.Value ?? "";
        return View(vm);
    }

    [HttpPost("/playground/text-gen")]
    public async Task<ActionResult> TextGen(
        [FromForm] TextGenPlaygroundViewModel data,
        [FromServices] IServiceFactory<ITextGenService> textGenFactory,
        [FromServices] IServiceFactory<IActionInferenceService> actionInferenceServiceFactory,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        if (profile == null) return RedirectToAction("Settings", "Settings");
        if (string.IsNullOrEmpty(data.Character)) return BadRequest("Character is required");
        var characterId = Guid.Parse(data.Character);
        var character = await _characterRepository.GetCharacterAsync(characterId, cancellationToken);
        if (character == null) throw new NullReferenceException("Could not find character");
        var vm = await GetTextGenViewModel(profile, data.Service, cancellationToken, character);
        vm.Prompt = data.Prompt ?? "";
        
        if (!ModelState.IsValid)
            return View(vm);
        ModelState.Clear();
        
        string result;
        try
        {
            ServiceLink? link;
            if (string.IsNullOrEmpty(vm.Service))
            {
                link = character.Services.TextGen.Service;
            }
            else
            {
                var cs = Guid.TryParse(vm.Service, out var serviceId)
                    ? await _serviceRepository.GetServiceByIdAsync(serviceId, cancellationToken)
                    : await _serviceRepository.GetServiceByNameAsync(vm.Service, cancellationToken);
                if (cs == null)
                    throw new NullReferenceException($"Could not find service {vm.Service}");
                link = new ServiceLink(cs);
            }

            if (data.Template == "Reply")
            {
                var textGen = await textGenFactory.CreateBestMatchRequiredAsync(profile.TextGen, link, Array.Empty<string>(), vm.Culture ?? "en-US", cancellationToken);
                var processor = new ChatTextProcessor(profile, character.Name, CultureInfo.GetCultureInfoByIetfLanguageTag(character.Culture), textGen);
                result = await textGen.GenerateReplyAsync(new ChatSessionData
                {
                    Chat = null!,
                    Culture = character.Culture,
                    User = new ChatSessionDataUser { Name = profile.Name },
                    Character = new ChatSessionDataCharacter
                    {
                        Name = processor.ProcessText(character.Name),
                        Description = processor.ProcessText(character.Description),
                        Personality = processor.ProcessText(character.Personality),
                        Scenario = processor.ProcessText(character.Scenario),
                    },
                    Messages =
                    {
                        ChatMessageData.FromText(Guid.Empty, character, character.FirstMessage),
                        ChatMessageData.FromText(Guid.Empty, profile, "Hi! This is a test conversation. Can you tell me something in character?")
                    }
                }, cancellationToken);
                vm.Service = _serviceObserver.GetRecord(ServiceObserverKeys.TextGenService)?.Value ?? data.Service ?? "";
                vm.Prompt = _serviceObserver.GetRecord(ServiceObserverKeys.TextGenPrompt)?.Value ?? "";
            }
            else if (data.Template == "ActionInference")
            {
                var svc = await actionInferenceServiceFactory.CreateBestMatchRequiredAsync(profile.ActionInference, link, Array.Empty<string>(), vm.Culture ?? "en-US", cancellationToken);
                result = await svc.SelectActionAsync(new ChatSessionData
                {
                    Chat = null!,
                    Culture = character.Culture,
                    User = new ChatSessionDataUser { Name = profile.Name },
                    Character = new ChatSessionDataCharacter
                    {
                        Name = character.Name,
                        Description = character.Description,
                        Personality = character.Personality,
                        Scenario = character.Scenario,
                    },
                    Actions = new[] { "wave", "sit_down", "stand_up", "break_chair" },
                    Messages =
                    {
                        ChatMessageData.FromText(Guid.Empty, character, character.FirstMessage),
                        ChatMessageData.FromText(Guid.Empty, profile, "Can you please sit down?")
                    }
                }, cancellationToken);
                vm.Service = _serviceObserver.GetRecord(ServiceObserverKeys.ActionInferenceService)?.Value ?? data.Service ?? "";
                vm.Prompt = vm.Service == OpenAIConstants.ServiceName
                    ? "System:\n" +
                      _serviceObserver.GetRecord($"{ServiceObserverKeys.ActionInferencePrompt}[System]")?.Value +
                      "\n\nUser:\n" +
                      _serviceObserver.GetRecord($"{ServiceObserverKeys.ActionInferencePrompt}[User]")?.Value
                    : _serviceObserver.GetRecord(ServiceObserverKeys.ActionInferencePrompt)?.Value ?? "";
            }
            else
            {
                var textGen = await textGenFactory.CreateBestMatchRequiredAsync(profile.TextGen, link, Array.Empty<string>(), vm.Culture ?? "en-US", cancellationToken);
                result = await textGen.GenerateAsync(vm.Prompt, cancellationToken);
                vm.Service = _serviceObserver.GetRecord(ServiceObserverKeys.TextGenService)?.Value ?? data.Service ?? "";
            }
        }
        catch (Exception exc)
        {
            result = exc.ToString();
        }

        vm.Response = result;
        var service = _serviceObserver.GetRecord(ServiceObserverKeys.TextGenService)?.Value;
        if (service != null)
            vm.Service = service;
        return View(vm);
    }

    private async Task<TextGenPlaygroundViewModel> GetTextGenViewModel(ProfileSettings profile, string? service, CancellationToken cancellationToken, Character? character)
    {
        var services = new[] { new OptionViewModel("", "Automatic") }
            .Concat(profile.TextGen.Services.Where(x => x.ServiceId != null).Select(x => OptionViewModel.Create(x.ServiceId.ToString()!, x.ServiceName)))
            .ToList();

        var characters = await _characterRepository.GetCharactersListAsync(cancellationToken);

        return new TextGenPlaygroundViewModel
        {
            Services = services,
            Service = service ?? character?.Services.TextGen.Service?.ServiceName,
            Characters = characters.Select(c => new OptionViewModel(c.Id.ToString(), c.Name)).ToList(),
            Character = character?.Id.ToString(),
            Culture = character?.Culture ?? "en-US",
        };
    }
}
