using System.Text;
using System.Text.Json;
using Voxta.Abstractions.Model;
using Voxta.Common;

namespace Voxta.Characters;

public static class TavernCardV2Import
{
    public static async Task<TavernCardV2> ExtractCardDataAsync(Stream stream)
    {
        var extractions = await PngChunkReader.ExtractPngChunksAsync(stream);
        if (extractions.Count == 0)
            throw new InvalidOperationException("No extractions found");

        var textExtractions = extractions
            .Where(d => d.Type == "tEXt")
            .Select(PngChunkReader.Decode)
            .ToList();

        if (textExtractions.Count == 0)
        {
            var names = string.Join(", ", extractions.Select(e => e.Type));
            throw new Exception($"No extractions of type tEXt found, found: {names}");
        }

        var extracted = textExtractions.First();

        var data = Convert.FromBase64String(extracted.Text);
        var utf8String = Encoding.UTF8.GetString(data);

        try
        {
            var result = JsonSerializer.Deserialize<TavernCardV2>(utf8String);
            if (result == null) throw new InvalidOperationException("Invalid character card: " + utf8String);
            if (result.Spec != "chara_card_v2") throw new InvalidOperationException("Invalid character card spec: " + utf8String);
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed parsing tavern data as JSON: {ex.Message}", ex);
        }
    }

    public static Character ConvertCardToCharacter(TavernCardData data)
    {
        return new Character
        {
            Id = data.Extensions.TryGetValue("voxta/charId", out var charId) && !string.IsNullOrEmpty(charId) ? Guid.Parse(charId) : Crypto.CreateCryptographicallySecureGuid(),
            Name = data.Name,
            Description = data.Description ?? "",
            Personality = data.Personality ?? "",
            Scenario = data.Scenario ?? "",
            MessageExamples = data.MesExample,
            FirstMessage = data.FirstMes ?? "",
            PostHistoryInstructions = data.PostHistoryInstructions,
            CreatorNotes = data.CreatorNotes,
            SystemPrompt = data.SystemPrompt,
            Culture = data.Extensions.TryGetValue("voxta/culture", out var culture) && !string.IsNullOrEmpty(culture) ? culture : "en-US",
            Prerequisites = data.Extensions.TryGetValue("voxta/prerequisites", out var prerequisites) && !string.IsNullOrEmpty(prerequisites) ? prerequisites.Split(',') : null,
            ReadOnly = false,
            Services = new CharacterServicesMap
            {
                TextGen = new ServiceMap
                {
                    Service = data.Extensions.TryGetValue("voxta/textgen/service", out var textGen) && !string.IsNullOrEmpty(textGen) ? textGen : ""
                },
                SpeechGen = new VoiceServiceMap
                {
                    Service = data.Extensions.TryGetValue("voxta/tts/service", out var ttsService) && !string.IsNullOrEmpty(ttsService) ? ttsService : "",
                    Voice = data.Extensions.TryGetValue("voxta/tts/voice", out var ttsVoice) && !string.IsNullOrEmpty(ttsVoice) ? ttsVoice : "Naia"
                },
            },
            Options = new()
            {
                EnableThinkingSpeech = data.Extensions.TryGetValue("voxta/options/enable_thinking_speech", out var enableThinkingSpeech) && enableThinkingSpeech == "true"
            },
        };
    }
}