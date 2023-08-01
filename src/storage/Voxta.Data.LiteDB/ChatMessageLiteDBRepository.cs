
using Voxta.Abstractions.Repositories;
using LiteDB;
using Voxta.Abstractions.Model;

namespace Voxta.Data.LiteDB;

public class ChatMessageLiteDBRepository : IChatMessageRepository
{
    private readonly ILiteCollection<ChatMessageData> _chatMessagesCollection;

    public ChatMessageLiteDBRepository(ILiteDatabase db)
    {
        _chatMessagesCollection = db.GetCollection<ChatMessageData>();
    }
    
    public Task<ChatMessageData[]> GetChatMessagesAsync(Guid chatId, CancellationToken cancellationToken)
    {
        var messages = _chatMessagesCollection.Query()
            .Where(c => c.ChatId == chatId)
            .OrderBy(c => c.Timestamp)
            .ToArray();

        return Task.FromResult(messages);
    }

    public Task SaveMessageAsync(ChatMessageData message)
    {
        _chatMessagesCollection.Insert(message);
        return Task.CompletedTask;
    }

    public Task DeleteChatMessages(Guid chatId)
    {
        _chatMessagesCollection.DeleteMany(x => x.ChatId == chatId);
        return Task.CompletedTask;
    }
}