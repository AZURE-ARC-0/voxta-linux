using System.Text;
using System.Text.Json;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Characters;
using Voxta.Common;
using Voxta.Server.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Voxta.Core;
using Voxta.Server.ViewModels.Characters;
#if(WINDOWS)
#endif

namespace Voxta.Server.Controllers;

[Controller]
public class CharactersController : Controller
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly ILogger<CharactersController> _logger;
    private readonly IProfileRepository _profileRepository;
    private readonly IServicesRepository _servicesRepository;
    private readonly IServiceDefinitionsRegistry _servicesDefinitionsRegistry;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public CharactersController(ICharacterRepository characterRepository, IProfileRepository profileRepository, IMemoryRepository memoryRepository, ILogger<CharactersController> logger, IServicesRepository servicesRepository, IServiceDefinitionsRegistry servicesDefinitionsRegistry, IWebHostEnvironment hostingEnvironment)
    {
        _characterRepository = characterRepository;
        _profileRepository = profileRepository;
        _memoryRepository = memoryRepository;
        _logger = logger;
        _servicesRepository = servicesRepository;
        _servicesDefinitionsRegistry = servicesDefinitionsRegistry;
        _hostingEnvironment = hostingEnvironment;
    }
    
    [HttpGet("/characters")]
    public async Task<IActionResult> Characters(CancellationToken cancellationToken)
    {
        var model = await _characterRepository.GetCharactersListAsync(cancellationToken);
        return View(model);
    }
    
    [HttpGet("/characters/{charId}")]
    public async Task<IActionResult> Character(
        [FromRoute] string charId,
        [FromQuery] Guid? from,
        [FromServices] IServiceFactory<ITextToSpeechService> ttsServiceFactory,
        CancellationToken cancellationToken
        )
    {
        var isNew = charId == "new";
        Character? character;
        if (charId == "new" && from == null)
        {
            character = new Character
            {
                Id = Crypto.CreateCryptographicallySecureGuid(),
                ReadOnly = false,
                Name = "",
                Description = "",
                Personality = "",
                Scenario = "",
                FirstMessage = "",
                MessageExamples = "",
                SystemPrompt = "",
                PostHistoryInstructions = "",
                Services = new CharacterServicesMap
                {
                    SpeechGen = new VoiceServiceMap
                    {
                        Voice = SpecialVoices.Undefined,
                    },
                },
                Options = new CharacterOptions
                {
                    EnableThinkingSpeech = true,
                }
            };
        }
        else
        {
            character = await _characterRepository.GetCharacterAsync(from ?? Guid.Parse(charId), cancellationToken);
            if (character == null)
                return NotFound("Character not found");
            if (isNew)
            {
                character.Id = Crypto.CreateCryptographicallySecureGuid();
                character.ReadOnly = false;
            }
        }
        
        var vm = await GenerateCharacterViewModelAsync(ttsServiceFactory, character, isNew, cancellationToken);

        return View(vm);
    }
    
    [HttpPost("/characters/delete")]
    public async Task<IActionResult> Delete([FromForm] Guid charId)
    {
        await _characterRepository.DeleteAsync(charId);
        var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "characters", charId + ".png");
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
        return RedirectToAction("Characters");
    }
    
    [HttpPost("/characters/{charId}")]
    public async Task<IActionResult> Character(
        [FromRoute] string charId,
        [FromForm] CharacterViewModel data,
        [FromServices] IServiceFactory<ITextToSpeechService> ttsServiceFactory,
        CancellationToken cancellationToken
    )
    {
        if (!ModelState.IsValid)
        {
            var isNew = charId == "new";
            var vm = await GenerateCharacterViewModelAsync(ttsServiceFactory, data.Character, isNew, cancellationToken);
            return View(vm);
        }

        if (charId != "new" && Guid.Parse(charId) != data.Character.Id)
            return BadRequest("Character ID mismatch");

        var prerequisites = new List<string>();
        if (data.PrerequisiteNSFW) prerequisites.Add(ServiceFeatures.NSFW);
        if (prerequisites.Count > 0) data.Character.Prerequisites = prerequisites.ToArray();
        if (!string.IsNullOrEmpty(data.TextGen))
        {
            var textGen = data.TextGen.Split("/");
            data.Character.Services.TextGen.Service = new ServiceLink(textGen[0], Guid.Parse(textGen[1]));
        }
        if (!string.IsNullOrEmpty(data.TextToSpeech))
        {
            var tts = data.TextToSpeech.Split("/");
            data.Character.Services.SpeechGen.Service = new ServiceLink(tts[0], Guid.Parse(tts[1]));
            data.Character.Services.SpeechGen.Voice = data.Voice;
        }

        if (data.AvatarUpload != null)
        {
            var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "characters", charId + ".png");
            await using (var stream = new FileStream(path, FileMode.Create))
                await data.AvatarUpload.CopyToAsync(stream, cancellationToken);

            data.Character.AvatarUrl = $"/uploads/characters/{charId}.png";
        }
        
        await _characterRepository.SaveCharacterAsync(data.Character);
        return RedirectToAction("Characters");
    }

    private async Task<CharacterViewModelWithOptions> GenerateCharacterViewModelAsync(IServiceFactory<ITextToSpeechService> ttsServiceFactory, Character character, bool isNew, CancellationToken cancellationToken)
    {
        VoiceInfo[] voices; 

        if (character.Services.SpeechGen.Service != null)
        {
            try
            {
                var profile = await _profileRepository.GetRequiredProfileAsync(cancellationToken);
                var prerequisites = profile.IgnorePrerequisites ? IgnorePrerequisitesValidator.Instance : new PrerequisitesValidator(character);
                var ttsService = await ttsServiceFactory.CreateBestMatchRequiredAsync(profile.TextToSpeech, character.Services.SpeechGen.Service, prerequisites, character.Culture, cancellationToken);
                voices = await ttsService.GetVoicesAsync(cancellationToken);
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Failed to get voices");
                voices = VoiceInfo.DefaultVoices;
            }
        }
        else
        {
            voices = VoiceInfo.DefaultVoices;
        }

        var services = await _servicesRepository.GetServicesAsync(cancellationToken);

        var vm = new CharacterViewModelWithOptions
        {
            IsNew = isNew,
            Character = character,
            PrerequisiteNSFW = character.Prerequisites?.Contains(ServiceFeatures.NSFW) == true,
            TextGenServices = new[] { new OptionViewModel("", "Select automatically") }
                .Concat(services
                    .Where(x => _servicesDefinitionsRegistry.Get(x.ServiceName).TextGen.IsSupported())
                    .Select(x => new OptionViewModel($"{x.ServiceName}/{x.Id}", x.ServiceName))
                )
                .ToArray(),
            TextGen = character.Services.TextGen.Service != null ? $"{character.Services.TextGen.Service.ServiceName}/{character.Services.TextGen.Service.ServiceId}" : "",
            TextToSpeechServices = new[] { new OptionViewModel("", "Select automatically") }
                .Concat(services
                    .Where(x => _servicesDefinitionsRegistry.Get(x.ServiceName).TTS.IsSupported())
                    .Select(x => new OptionViewModel($"{x.ServiceName}/{x.Id}", x.ServiceName))
                )
                .ToArray(),
            TextToSpeech = character.Services.SpeechGen.Service != null ? $"{character.Services.SpeechGen.Service.ServiceName}/{character.Services.SpeechGen.Service.ServiceId}" : "",
            Voice = character.Services.SpeechGen.Voice,
            Cultures = CultureUtils.Bcp47LanguageTags.Select(c => new OptionViewModel(c.Name, c.Label)).ToArray(),
            Voices = voices,
        };

        return vm;
    }
    
    [HttpPost("/characters/import")]
    public async Task<IActionResult> Upload(IFormFile[] files)
    {
        if (files is not { Length: 1 }) throw new Exception("File required");

        var file = files[0];
        await using var stream = file.OpenReadStream();
        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        await stream.DisposeAsync();
        
        var card = Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".json" => JsonSerializer.Deserialize<TavernCardV2>(ms),
            ".png" => await TavernCardV2Import.ExtractCardDataAsync(ms),
            _ => throw new NotSupportedException($"Unsupported file type: {Path.GetExtension(file.FileName)}"),
        };
        if (card?.Data == null) throw new InvalidOperationException("Invalid V2 card file: no data");

        var character = TavernCardV2Import.ConvertCardToCharacter(card.Data);
        await _characterRepository.SaveCharacterAsync(character);

        var book = TavernCardV2Import.ConvertBook(character.Id, card.Data.CharacterBook);
        if (book != null)
        {
            await _memoryRepository.SaveBookAsync(book);
        }
        
        ms.Seek(0, SeekOrigin.Begin);
        // Write the contents of ms to the wwwroot/uploads/characters/{characterId}.png file
        var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "characters", character.Id + ".png");
        await using (var fileStream = new FileStream(path, FileMode.Create))
            await ms.CopyToAsync(fileStream);
        character.AvatarUrl = $"/uploads/characters/{character.Id}.png";
        
        return RedirectToAction("Character", new { charId = character.Id });
    }



    [HttpGet("/characters/{charId:guid}/download")]
    public async Task<IActionResult> Download([FromRoute] Guid charId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterAsync(charId, cancellationToken);
        if (character == null) return NotFound();
        var book = await _memoryRepository.GetCharacterBookAsync(character.Id);
        var card = TavernCardV2Export.ConvertCharacterToCard(character, book);
        // Serialize card to string and download as a json file attachment
        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"{character.Name}.json");
    }
}
