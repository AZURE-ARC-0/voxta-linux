namespace Voxta.Abstractions.Model;

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