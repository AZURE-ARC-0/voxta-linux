using System.Text;
using System.Text.Json;

namespace ChatMate.Characters;

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
            return result ?? throw new InvalidOperationException("Invalid character card: " + utf8String);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed parsing tavern data as JSON: {ex.Message}", ex);
        }
    }
}