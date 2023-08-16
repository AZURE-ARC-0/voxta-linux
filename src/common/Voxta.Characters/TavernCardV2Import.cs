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
        var prerequisites = GetPrerequisites(data);
        var charIdValue =  GetString(data.Extensions, "voxta/charId", Crypto.CreateCryptographicallySecureGuid().ToString());
        var cultureValue = GetString(data.Extensions, "voxta/culture", "en-US");
        // var textGenValue = GetString(data.Extensions, "voxta/textgen/service", "");
        // var ttsValue = GetString(data.Extensions, "voxta/tts/service", "");
        // var voiceValue = GetString(data.Extensions, "voxta/tts/voice", "");
        var thinkingSpeechValue = GetString(data.Extensions, "voxta/options/enable_thinking_speech", "true") == "true";
        
        return new Character
        {
            Id = Guid.Parse(charIdValue),
            Name = data.Name,
            Description = data.Description ?? "",
            Personality = data.Personality ?? "",
            Scenario = data.Scenario ?? "",
            MessageExamples = data.MesExample,
            FirstMessage = data.FirstMes ?? "",
            PostHistoryInstructions = data.PostHistoryInstructions,
            CreatorNotes = data.CreatorNotes,
            SystemPrompt = data.SystemPrompt,
            Culture = cultureValue,
            Prerequisites = prerequisites,
            ReadOnly = false,
            #warning Would be nice to keep the service type at least
            Services = new CharacterServicesMap
            {
                // TextGen = new ServiceMap
                // {
                //     Service = textGenValue
                // },
                // SpeechGen = new VoiceServiceMap
                // {
                //     Service = ttsValue,
                //     Voice = voiceValue
                // },
            },
            Options = new()
            {
                EnableThinkingSpeech = thinkingSpeechValue
            },
        };
    }

    private static string GetString(IReadOnlyDictionary<string, dynamic> data, string key, string defaultValue)
    {
        if (!data.TryGetValue(key, out var value))
            return defaultValue;
        var valueString = value.ToString();
        if (string.IsNullOrEmpty(valueString)) return defaultValue;
        return valueString;
    }

    private static string[]? GetPrerequisites(TavernCardData data)
    {
        if (!data.Extensions.TryGetValue("voxta/prerequisites", out var prerequisites))
        {
            if (data.Tags != null && data.Tags.Contains("nsfw", StringComparer.InvariantCultureIgnoreCase))
                return new[] { ServiceFeatures.NSFW };
        }

        string? prerequisitesString = prerequisites?.ToString();
        if (string.IsNullOrEmpty(prerequisitesString))
            return null;
        return prerequisitesString.Split(',');
    }

    public static MemoryBook? ConvertBook(Guid characterId, CharacterBook? data)
    {
        if (data == null) return null;
        var bookIdValue =  GetString(data.Extensions, "voxta/bookId", Crypto.CreateCryptographicallySecureGuid().ToString());
        return new MemoryBook
        {
            Id = Guid.Parse(bookIdValue),
            CharacterId = characterId,
            Name = data.Name,
            Description = data.Description,
            Items = data.Entries.Select(x => new MemoryItem
            {
                Id = Guid.NewGuid(),
                Keywords = x.Keys,
                Text = x.Content,
                Weight = x.Priority,
            }).ToList()
        };
    }
}