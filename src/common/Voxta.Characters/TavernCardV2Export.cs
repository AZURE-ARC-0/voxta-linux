﻿using Voxta.Abstractions.Model;

namespace Voxta.Characters;

public static class TavernCardV2Export
{
    public static TavernCardV2 ConvertCharacterToCard(Character character)
    {
        var card = new TavernCardV2
        {
            Spec = "chara_card_v2",
            SpecVersion = "2.0",
            Data = new TavernCardData
            {
                Name = character.Name,
                Description = character.Description,
                Personality = character.Personality,
                Scenario = character.Scenario,
                FirstMes = character.FirstMessage,
                MesExample = character.MessageExamples,
                CreatorNotes = character.CreatorNotes,
                SystemPrompt = character.SystemPrompt,
                PostHistoryInstructions = character.PostHistoryInstructions,
                Creator = character.Creator,
                Tags = character.Tags,
                Extensions = new Dictionary<string, dynamic>
                {
                    { "voxta/prerequisites", character.Prerequisites != null ? string.Join(",", character.Prerequisites) : "" },
                    { "voxta/culture", character.Culture },
                    { "voxta/textgen/service", character.Services.TextGen.Service ?? "" },
                    { "voxta/tts/service", character.Services.SpeechGen.Service ?? "" },
                    { "voxta/tts/voice", character.Services.SpeechGen.Voice ?? "" },
                    { "voxta/options/enable_thinking_speech", character.Options?.EnableThinkingSpeech ?? true ? "true" : "false" },
                }
            }
        };
        return card;
    }
}