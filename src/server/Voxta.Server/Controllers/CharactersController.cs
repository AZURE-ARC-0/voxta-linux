using System.Text;
using System.Text.Json;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Characters;
using Voxta.Common;
using Voxta.Server.ViewModels;
using Voxta.Services.KoboldAI;
using Voxta.Services.ElevenLabs;
using Voxta.Services.Mocks;
using Voxta.Services.NovelAI;
using Voxta.Services.Oobabooga;
using Voxta.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Voxta.Server.ViewModels.Characters;
using Voxta.Services.AzureSpeechService;
using Voxta.Services.TextGenerationInference;
#if(WINDOWS)
using Voxta.Services.WindowsSpeech;
#endif

namespace Voxta.Server.Controllers;

[Controller]
public class CharactersController : Controller
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly ILogger<CharactersController> _logger;
    private readonly IProfileRepository _profileRepository;

    public CharactersController(ICharacterRepository characterRepository, IProfileRepository profileRepository, IMemoryRepository memoryRepository, ILogger<CharactersController> logger)
    {
        _characterRepository = characterRepository;
        _profileRepository = profileRepository;
        _memoryRepository = memoryRepository;
        _logger = logger;
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
                    TextGen = new ServiceMap
                    {
                        Service = "",
                    },
                    SpeechGen = new VoiceServiceMap
                    {
                        Service = "",
                        Voice = "",
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
        if (data.PrerequisiteGPT3) prerequisites.Add(ServiceFeatures.GPT3);
        if (prerequisites.Count > 0) data.Character.Prerequisites = prerequisites.ToArray();
        
        await _characterRepository.SaveCharacterAsync(data.Character);
        return RedirectToAction("Character", new { characterId = data.Character.Id });
    }

    private async Task<CharacterViewModelWithOptions> GenerateCharacterViewModelAsync(IServiceFactory<ITextToSpeechService> ttsServiceFactory, Character character, bool isNew, CancellationToken cancellationToken)
    {
        VoiceInfo[] voices; 

        if (!string.IsNullOrEmpty(character.Services.SpeechGen.Service))
        {
            try
            {
                var profile = await _profileRepository.GetRequiredProfileAsync(cancellationToken);
                var ttsService = await ttsServiceFactory.CreateBestMatchAsync(profile.TextToSpeech, character.Services.SpeechGen.Service,
                    character.Prerequisites ?? Array.Empty<string>(), character.Culture, cancellationToken);
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

        var vm = new CharacterViewModelWithOptions
        {
            IsNew = isNew,
            Character = character,
            PrerequisiteNSFW = character.Prerequisites?.Contains(ServiceFeatures.NSFW) == true,
            PrerequisiteGPT3 = character.Prerequisites?.Contains(ServiceFeatures.GPT3) == true,
            TextGenServices = new[]
            {
                new OptionViewModel("", "Select automatically"),
                OptionViewModel.Create(OpenAIConstants.ServiceName),
                OptionViewModel.Create(NovelAIConstants.ServiceName),
                OptionViewModel.Create(TextGenerationInferenceConstants.ServiceName),
                OptionViewModel.Create(OobaboogaConstants.ServiceName),
                OptionViewModel.Create(KoboldAIConstants.ServiceName),
                #if(DEBUG)
                OptionViewModel.Create(MockConstants.ServiceName),
                #endif
            },
            TextToSpeechServices = new[]
            {
                new OptionViewModel("", "Select automatically"),
                OptionViewModel.Create(NovelAIConstants.ServiceName),
                OptionViewModel.Create(ElevenLabsConstants.ServiceName),
                OptionViewModel.Create(AzureSpeechServiceConstants.ServiceName),
                #if(WINDOWS)
                OptionViewModel.Create(WindowsSpeechConstants.ServiceName),
                #endif
                #if(DEBUG)
                OptionViewModel.Create(MockConstants.ServiceName),
                #endif
            },
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
        var card = Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".json" => JsonSerializer.Deserialize<TavernCardV2>(stream),
            ".png" => await TavernCardV2Import.ExtractCardDataAsync(stream),
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
