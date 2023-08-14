namespace Voxta.Abstractions.Model;

public static class ChatSessionDataExtensions
{
    public static ChatMessageData AddMessage(this ChatSessionData chat, ChatSessionDataCharacter character, TextData message)
    {
        var msg = ChatMessageData.FromGen(chat.Chat.Id, character, message);
        chat.Messages.Add(msg);
        return msg;
    }
    
    public static ChatMessageData AddMessage(this ChatSessionData chat, ChatSessionDataCharacter character, string message)
    {
        var msg = ChatMessageData.FromText(chat.Chat.Id, character, message);
        chat.Messages.Add(msg);
        return msg;
    }
    
    public static ChatMessageData AddMessage(this ChatSessionData chat, ChatSessionDataUser user, TextData message)
    {
        var msg = ChatMessageData.FromGen(chat.Chat.Id, user, message);
        chat.Messages.Add(msg);
        return msg;
    }
    
    public static ChatMessageData AddMessage(this ChatSessionData chat, ChatSessionDataUser user, string message)
    {
        var msg = ChatMessageData.FromText(chat.Chat.Id, user, message);
        chat.Messages.Add(msg);
        return msg;
    }
}