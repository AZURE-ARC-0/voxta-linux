namespace Voxta.Abstractions.Model;

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