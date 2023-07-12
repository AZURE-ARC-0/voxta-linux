using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using ChatMate.Server.ViewModels;
using ChatMate.Services.KoboldAI;
using ChatMate.Services.ElevenLabs;
using ChatMate.Services.NovelAI;
using ChatMate.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers.Pages;

[Controller]
public class CharactersController : Controller
{
    [HttpGet("/characters")]
    public async Task<IActionResult> Characters([FromServices] ICharacterRepository charactersRepository, CancellationToken cancellationToken)
    {
        var model = await charactersRepository.GetCharactersListAsync(cancellationToken);
        return View(model);
    }
    
    [HttpGet("/characters/{charId}")]
    public async Task<IActionResult> Character(
        [FromRoute] string charId,
        [FromQuery] string? from,
        [FromServices] ICharacterRepository charactersRepository,
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
                Name = "",
                Description = "",
                Personality = "",
                Scenario = "",
                FirstMessage = "",
                MessageExamples = "",
                SystemPrompt = "",
                PostHistoryInstructions = "",
                Services = new Character.ServicesMap
                {
                    TextGen = new Character.ServiceMap
                    {
                        Service = OpenAIConstants.ServiceName,
                    },
                    SpeechGen = new Character.VoiceServiceMap
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
            character = await charactersRepository.GetCharacterAsync(from ?? charId, cancellationToken);
            if (character == null)
                return NotFound("Character not found");
            if (isNew) character.Id = Crypto.CreateCryptographicallySecureGuid().ToString();
        }

        var vm = await GenerateCharacterViewModelAsync(ttsServiceFactory, character, isNew, cancellationToken);

        return View(vm);
    }

    [HttpPost("/characters/{charId}")]
    public async Task<IActionResult> Character(
        [FromRoute] string charId,
        [FromForm] CharacterViewModel data,
        [FromServices] ICharacterRepository charactersRepository,
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
        await charactersRepository.SaveCharacterAsync(data.Character);
        return RedirectToAction("Character", new { characterId = data.Character.Id });
    }

    private static async Task<CharacterViewModelWithOptions> GenerateCharacterViewModelAsync(IServiceFactory<ITextToSpeechService> ttsServiceFactory, Character character, bool isNew,
        CancellationToken cancellationToken)
    {
        var vm = new CharacterViewModelWithOptions
        {
            IsNew = isNew,
            Character = character,
            TextGenServices = new[]
            {
                OpenAIConstants.ServiceName,
                NovelAIConstants.ServiceName,
                KoboldAIConstants.ServiceName,
            },
            TextToSpeechServices = new[]
            {
                NovelAIConstants.ServiceName,
                ElevenLabsConstants.ServiceName,
            },
        };

        if (!string.IsNullOrEmpty(character.Services.SpeechGen.Service))
        {
            var ttsService = await ttsServiceFactory.CreateAsync(character.Services.SpeechGen.Service, cancellationToken);
            vm.Voices = await ttsService.GetVoicesAsync(cancellationToken);
        }

        return vm;
    }
}