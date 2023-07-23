﻿using Voxta.Abstractions.DependencyInjection;
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
        await _characterRepository.SaveCharacterAsync(data.Character);
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
                OobaboogaConstants.ServiceName,
                FakesConstants.ServiceName,
            },
            TextToSpeechServices = new[]
            {
                NovelAIConstants.ServiceName,
                ElevenLabsConstants.ServiceName,
                FakesConstants.ServiceName,
            },
        };

        if (!string.IsNullOrEmpty(character.Services.SpeechGen.Service))
        {
            var ttsService = await ttsServiceFactory.CreateAsync(character.Services.SpeechGen.Service, cancellationToken);
            vm.Voices = await ttsService.GetVoicesAsync(cancellationToken);
        }

        return vm;
    }
    
    [HttpPost("/characters/import")]
    public async Task<IActionResult> Upload(IFormFile[] files)
    {
        if (files is not { Length: 1 }) throw new Exception("File required");

        var file = files[0];
        await using var stream = file.OpenReadStream();
        var card = await TavernCardV2Import.ExtractCardDataAsync(stream);
        if (card.Data == null) throw new InvalidOperationException("Invalid V2 card file: no data");

        var character = new Character
        {
            Name = card.Data.Name,
            Description = card.Data.Description ?? "",
            Personality = card.Data.Personality ?? "",
            Scenario = card.Data.Scenario ?? "",
            MessageExamples = card.Data.MesExample,
            FirstMessage = card.Data.FirstMes,
            PostHistoryInstructions = card.Data.PostHistoryInstructions,
            CreatorNotes = card.Data.CreatorNotes,
            Id = Crypto.CreateCryptographicallySecureGuid().ToString(),
            SystemPrompt = card.Data.SystemPrompt,
            ReadOnly = false,
            Services = new Character.CharacterServicesMap
            {
                TextGen = new ServiceMap
                {
                    Service = NovelAIConstants.ServiceName
                },
                SpeechGen = new VoiceServiceMap
                {
                    Service = NovelAIConstants.ServiceName,
                    Voice = "Naia"
                },
            },
            Options = new()
            {
                EnableThinkingSpeech = false,
            },
        };

        await _characterRepository.SaveCharacterAsync(character);
        
        return RedirectToAction("Character", new { charId = character.Id });
    }
}