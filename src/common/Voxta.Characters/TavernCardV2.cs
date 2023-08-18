using System.Text.Json.Serialization;

namespace Voxta.Characters;

[Serializable]
public class TavernCardV2
{
    [JsonPropertyName("spec")]
    public string? Spec { get; set; }

    [JsonPropertyName("spec_version")]
    public string? SpecVersion { get; set; }

    [JsonPropertyName("data")]
    public TavernCardData? Data { get; set; }
}

[Serializable]
public class TavernCardData
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("personality")]
    public string? Personality { get; set; }

    [JsonPropertyName("scenario")]
    public string? Scenario { get; set; }

    [JsonPropertyName("first_mes")]
    public string? FirstMes { get; set; }

    [JsonPropertyName("mes_example")]
    public string? MesExample { get; set; }

    [JsonPropertyName("creator_notes")]
    public string? CreatorNotes { get; set; }

    [JsonPropertyName("system_prompt")]
    public string? SystemPrompt { get; set; }

    [JsonPropertyName("post_history_instructions")]
    public string? PostHistoryInstructions { get; set; }

    [JsonPropertyName("alternate_greetings")]
    public List<string>? AlternateGreetings { get; set; }

    [JsonPropertyName("character_book")]
    public CharacterBook? CharacterBook { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    [JsonPropertyName("character_version")]
    public string? CharacterVersion { get; set; }

    [JsonPropertyName("extensions")]
    public Dictionary<string, dynamic> Extensions { get; set; } = new();
}

[Serializable]
public class CharacterBook
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("extensions")]
    public Dictionary<string, dynamic> Extensions { get; set; } = new();
    
    [JsonPropertyName("entries")]
    public CharacterBookEntry[] Entries { get; set; } = Array.Empty<CharacterBookEntry>();
}

[Serializable]
public class CharacterBookEntry
{
    [JsonPropertyName("keys")]
    public required string[] Keys { get; set; }
    
    [JsonPropertyName("Content")]
    public required string Content { get; set; }
    
    [JsonPropertyName("extensions")]
    public Dictionary<string, dynamic> Extensions { get; set; } = new();
    
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("insertion_order")]
    public int InsertionOrder { get; set; } = 0;
    
    [JsonPropertyName("case_sensitive")]
    public bool CaseSensitive { get; set; } = false;
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;
}
