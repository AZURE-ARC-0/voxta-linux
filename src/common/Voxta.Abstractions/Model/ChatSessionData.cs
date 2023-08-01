﻿namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatSessionData : IChatInferenceData
{
    public required Chat Chat { get; init; }
    public required string UserName { get; init; }
    public required CharacterCardExtended Character { get; init; }
    public string? Context { get; set; }
    public string[]? Actions { get; set; }
    public string[]? ThinkingSpeech { get; init; }

    public IReadOnlyList<ChatMessageData> GetMessages() => Messages.AsReadOnly();

    public List<ChatMessageData> Messages { get; } = new();
    
    public string? AudioPath { get; init; }

    public string GetMessagesAsString()
    {
        return string.Join("\n", Messages.Select(m => $"{m.User}: {m.Text}"));
    }
}

public static class ChatSessionDataExtensions
{
    public static ChatMessageData AddMessage(this ChatSessionData chat, string user, TextData message)
    {
        var msg = ChatMessageData.FromGen(chat.Chat.Id, user, message);
        chat.Messages.Add(msg);
        return msg;
    }
    
    public static ChatSessionData AddMessage(this ChatSessionData chat, string user, string message)
    {
        chat.Messages.Add(ChatMessageData.FromText(chat.Chat.Id, user, message));
        return chat;
    }
}