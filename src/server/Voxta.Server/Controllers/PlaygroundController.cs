using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Voxta.Server.ViewModels;
using Voxta.Server.ViewModels.Playground;

namespace Voxta.Server.Controllers;

[Controller]
public class PlaygroundController : Controller
{
    private readonly IProfileRepository _profileRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IServiceObserver _serviceObserver;

    public PlaygroundController(IProfileRepository profileRepository, ICharacterRepository characterRepository, IServiceObserver serviceObserver)
    {
        _profileRepository = profileRepository;
        _characterRepository = characterRepository;
        _serviceObserver = serviceObserver;
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
            Services = profile.TextToSpeech.Services.Select(OptionViewModel.Create).ToList(),
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
    public async Task<ActionResult> TextGen([FromForm] TextGenPlaygroundViewModel data, [FromServices] IServiceFactory<ITextGenService> textGenFactory, CancellationToken cancellationToken)
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
        
        var textGen = await textGenFactory.CreateBestMatchAsync(profile.TextGen, vm.Service ?? "", Array.Empty<string>(), vm.Culture ?? "en-US", cancellationToken);
        string result;
        if (data.Template == "Reply")
        {
            result = await textGen.GenerateReplyAsync(new ChatSessionData
            {
                Chat = null!,
                UserName = profile.Name,
                Character = character,
                Messages =
                {
                    ChatMessageData.FromText(Guid.Empty, character.Name, character.FirstMessage),
                    ChatMessageData.FromText(Guid.Empty, profile.Name, "Hi! This is a test conversation. Can you tell me something in character?")
                }
            }, cancellationToken);
            vm.Prompt = _serviceObserver.GetRecord(ServiceObserverKeys.TextGenPrompt)?.Value ?? "";
        }
        else if (data.Template == "ActionInference")
        {
            throw new NotImplementedException("This template was not implemented");
        }
        else
        {
            result = await textGen.GenerateAsync(vm.Prompt, cancellationToken);
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
            .Concat(profile.TextGen.Services.Select(OptionViewModel.Create))
            .ToList();

        var characters = await _characterRepository.GetCharactersListAsync(cancellationToken);

        return new TextGenPlaygroundViewModel
        {
            Services = services,
            Service = service ?? character?.Services.TextGen.Service,
            Characters = characters.Select(c => new OptionViewModel(c.Id.ToString(), c.Name)).ToList(),
            Character = character?.Id.ToString(),
            Culture = character?.Culture ?? "en-US",
        };
    }
}
