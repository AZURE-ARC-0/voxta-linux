using System.Globalization;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatSessionData : IChatInferenceData
{
    public required Chat Chat { get; init; }
    public required string Culture { get; set; } = "en-US";
    public CultureInfo CultureInfo => CultureInfo.GetCultureInfoByIetfLanguageTag(Culture);
    public required ChatSessionDataUser User { get; init; }

    public required ChatSessionDataCharacter Character { get; init; }
    public TextData? Context { get; set; }
    public string[]? Actions { get; set; }
    public string[]? ThinkingSpeech { get; init; }

    public IReadOnlyList<ChatMessageData> GetMessages() => Messages.AsReadOnly();
    public List<ChatMessageData> Messages { get; } = new();

    public string? AudioPath { get; init; }
    
    public IReadOnlyList<ChatSessionDataMemory> GetMemories() => Memories.AsReadOnly();
    public List<ChatSessionDataMemory> Memories { get; init; } = new();

    public string GetMessagesAsString()
    {
        return string.Join("\n", Messages.Select(m => $"{m.User}: {m.Text}"));
    }
}

[Serializable]
public class ChatSessionDataCharacter
{
    public required TextData Name { get; init; }
    public required TextData Description { get; init; }
    public required TextData Personality { get; init; }
    public required TextData Scenario { get; init; }
    public TextData FirstMessage { get; init; } = TextData.Empty;
    public TextData MessageExamples { get; init; } = TextData.Empty;
    public TextData SystemPrompt { get; init; } = TextData.Empty;
    public TextData PostHistoryInstructions { get; init; } = TextData.Empty;
}

[Serializable]
public class ChatSessionDataUser
{
    public required TextData Name { get; init; }
    public TextData Description { get; init; } = TextData.Empty;
}

[Serializable]
public class ChatSessionDataMemory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required TextData Text { get; set; }
}