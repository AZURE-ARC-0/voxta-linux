namespace ChatMate.Server;

public class ChatData
{
    public string BotName { get; init; } = "Me";
    public string UserName { get; init; } = "User";
    public string Preamble { get; init; } = """
        [ Title: Romance Dialogue; Genre: Conversation ]
        Personality: Empathetic, flirty, friendly, funny, romantic, and talkative.
        Scenario: I am a virtual companion, and I live in a VR world for the sole benefit of my user. I really like the user.
        """;

    public List<ChatMessageData> Messages { get; } = new();
}

public class ChatMessageData
{
    public required string User { get; init; }
    public required string Text { get; init; }
}