using System.Text;
using System.Text.Json;
using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Characters;
using Voxta.Common;
using Voxta.Server.ViewModels;
using Voxta.Services.KoboldAI;
using Voxta.Services.ElevenLabs;
using Voxta.Services.Fakes;
using Voxta.Services.NovelAI;
using Voxta.Services.Oobabooga;
using Voxta.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Voxta.Services.AzureSpeechService;

namespace Voxta.Server.Controllers;

[Controller]
public class CharactersController : Controller
{
    private readonly ICharacterRepository _characterRepository;

    public CharactersController(ICharacterRepository characterRepository)
    {
        _characterRepository = characterRepository;
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
        [FromQuery] string? from,
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
                Id = Crypto.CreateCryptographicallySecureGuid().ToString(),
                ReadOnly = false,
                Name = "",
                Description = "",
                Personality = "",
                Scenario = "",
                FirstMessage = "",
                MessageExamples = "",
                SystemPrompt = "",
                PostHistoryInstructions = "",
                Services = new Character.CharacterServicesMap
                {
                    TextGen = new ServiceMap
                    {
                        Service = OpenAIConstants.ServiceName,
                    },
                    SpeechGen = new VoiceServiceMap
                    {
                        Service = NovelAIConstants.ServiceName,
                        Voice = "Naia",
                    },
                },
                Options = new Character.CharacterOptions
                {
                    EnableThinkingSpeech = true,
                }
            };
        }
        else
        {
            character = await _characterRepository.GetCharacterAsync(from ?? charId, cancellationToken);
            if (character == null)
                return NotFound("Character not found");
            if (isNew)
            {
                character.Id = Crypto.CreateCryptographicallySecureGuid().ToString();
                character.ReadOnly = false;
            }
        }
        
        var vm = await GenerateCharacterViewModelAsync(ttsServiceFactory, character, isNew, cancellationToken);

        return View(vm);
    }
    
    [HttpPost("/characters/delete")]
    public async Task<IActionResult> Delete([FromForm] string charId)
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

        if (charId != "new" && charId != data.Character.Id)
            return BadRequest("Character ID mismatch");

        var prerequisites = new List<string>();
        if (data.PrerequisiteNSFW) prerequisites.Add(Prerequisites.NSFW);
        if (prerequisites.Count > 0) data.Character.Prerequisites = prerequisites.ToArray();
        
        await _characterRepository.SaveCharacterAsync(data.Character);
        return RedirectToAction("Character", new { characterId = data.Character.Id });
    }

    private static async Task<CharacterViewModelWithOptions> GenerateCharacterViewModelAsync(IServiceFactory<ITextToSpeechService> ttsServiceFactory, Character character, bool isNew, CancellationToken cancellationToken)
    {
        VoiceInfo[] voices; 

        if (!string.IsNullOrEmpty(character.Services.SpeechGen.Service))
        {
            var ttsService = await ttsServiceFactory.CreateAsync(character.Services.SpeechGen.Service, character.Prerequisites ?? Array.Empty<string>(), character.Culture, cancellationToken);
            voices = await ttsService.GetVoicesAsync(cancellationToken);
        }
        else
        {
            voices = Array.Empty<VoiceInfo>();
        }

        var vm = new CharacterViewModelWithOptions
        {
            IsNew = isNew,
            Character = character,
            PrerequisiteNSFW = character.Prerequisites?.Contains(Prerequisites.NSFW) == true,
            TextGenServices = new[]
            {
                OpenAIConstants.ServiceName,
                NovelAIConstants.ServiceName,
                KoboldAIConstants.ServiceName,
                OobaboogaConstants.ServiceName,
                FakesConstants.ServiceName,
            },
            TextToSpeechServices = new[]
            {
                NovelAIConstants.ServiceName,
                ElevenLabsConstants.ServiceName,
                AzureSpeechServiceConstants.ServiceName,
                FakesConstants.ServiceName,
            },
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
        character.Id = Crypto.CreateCryptographicallySecureGuid().ToString();

        await _characterRepository.SaveCharacterAsync(character);
        
        return RedirectToAction("Character", new { charId = character.Id });
    }



    [HttpGet("/characters/{charId}/download")]
    public async Task<IActionResult> Download([FromRoute] string charId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterAsync(charId, cancellationToken);
        if (character == null) return NotFound();
        var card = TavernCardV2Export.ConvertCharacterToCard(character);
        // Serialize card to string and download as a json file attachment
        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"{character.Name}.json");
    }
}
