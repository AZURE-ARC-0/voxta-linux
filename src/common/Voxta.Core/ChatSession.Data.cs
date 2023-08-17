using Voxta.Abstractions.Model;

namespace Voxta.Core;

public partial class ChatSession
{
    private Task<ChatMessageData> AppendMessageAsync(ChatSessionDataCharacter character, TextData gen)
    {
        using var token = _chatSessionData.GetWriteToken();
        return SaveMessageAsync(token.AddMessage(character, gen));
    }
    
    private Task<ChatMessageData> AppendMessageAsync(ChatSessionDataUser user, TextData gen)
    {
        using var token = _chatSessionData.GetWriteToken();
        return SaveMessageAsync(token.AddMessage(user, gen));
    }
    
    private async Task<ChatMessageData> SaveMessageAsync(ChatMessageData message)
    {
        var perf = _performanceMetrics.Start("Database");
        await _chatMessageRepository.SaveMessageAsync(message);
        perf.Done();
        return message;
    }
    
    private async Task UpdateMessageAsync(ChatMessageData message)
    {
        var perf = _performanceMetrics.Start("Database");
        await _chatMessageRepository.UpdateMessageAsync(message);
        perf.Done();
    }
}
