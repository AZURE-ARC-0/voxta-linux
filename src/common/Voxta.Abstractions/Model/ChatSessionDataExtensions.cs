namespace Voxta.Abstractions.Model;

public static class ChatSessionDataWriteTokenExtensions
{
    public static ChatMessageData AddMessage(this ChatSessionDataWriteToken chat, ChatSessionDataCharacter character, TextData message)
    {
        var msg = ChatMessageData.FromGen(chat.Data.Id, character, message);
        chat.Messages.Add(msg);
        return msg;
    }
    
    public static ChatMessageData AddMessage(this ChatSessionDataWriteToken chat, ChatSessionDataCharacter character, string message)
    {
        var msg = ChatMessageData.FromText(chat.Data.Id, character, message);
        chat.Messages.Add(msg);
        return msg;
    }
    
    public static ChatMessageData AddMessage(this ChatSessionDataWriteToken chat, CharacterCard character, string message)
    {
        var msg = ChatMessageData.FromText(chat.Data.Id, character, message);
        chat.Messages.Add(msg);
        return msg;
    }
    
    public static ChatMessageData AddMessage(this ChatSessionDataWriteToken chat, ChatSessionDataUser user, TextData message)
    {
        var msg = ChatMessageData.FromGen(chat.Data.Id, user, message);
        chat.Messages.Add(msg);
        return msg;
    }
    
    public static ChatMessageData AddMessage(this ChatSessionDataWriteToken chat, ChatSessionDataUser user, string message)
    {
        var msg = ChatMessageData.FromText(chat.Data.Id, user, message);
        chat.Messages.Add(msg);
        return msg;
    }
    
    public static ChatMessageData AddMessage(this ChatSessionDataWriteToken chat, ProfileSettings user, string message)
    {
        var msg = ChatMessageData.FromText(chat.Data.Id, user, message);
        chat.Messages.Add(msg);
        return msg;
    }
}