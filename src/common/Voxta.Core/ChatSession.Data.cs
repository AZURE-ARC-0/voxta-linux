using Voxta.Abstractions.Model;

namespace Voxta.Core;

public partial class ChatSession
{
    private Task<ChatMessageData> SaveMessageAsync(string user, TextData gen)
    {
        return SaveMessageAsync(_chatSessionData.AddMessage(user, gen));
    }
    
    private async Task<ChatMessageData> SaveMessageAsync(ChatMessageData message)
    {
        var perf = _performanceMetrics.Start("Db");
        await _chatMessageRepository.SaveMessageAsync(message);
        perf.Done();
        return message;
    }
}
