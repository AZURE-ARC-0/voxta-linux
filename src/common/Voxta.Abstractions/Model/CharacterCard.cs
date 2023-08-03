using System.ComponentModel.DataAnnotations;

namespace Voxta.Abstractions.Model;

/// <summary>
/// See https://github.com/malfoyslastname/character-card-spec-v2 
/// </summary>
[Serializable]
public class CharacterCard
{
    public required string Name { get; init; }
    [Required(AllowEmptyStrings = true)]
    public required string Description { get; init; }
    [Required(AllowEmptyStrings = true)]
    public required string Personality { get; init; }
    [Required(AllowEmptyStrings = true)]
    public required string Scenario { get; init; }
    public required string FirstMessage { get; init; }
    public string? MessageExamples { get; init; }
    public string? SystemPrompt { get; init; }
    public string? PostHistoryInstructions { get; init; }
    
    public string? Creator { get; init; }
    public string? CreatorNotes { get; init; }
    public List<string>? Tags { get; set; }
}