
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using LiteDB;

namespace Voxta.Data.LiteDB;

public class ChatLiteDBRepository : IChatRepository
{
    private readonly ILiteCollection<Chat> _chatsCollection;

    public ChatLiteDBRepository(ILiteDatabase db)
    {
        _chatsCollection = db.GetCollection<Chat>();
    }
    
    public Task<Chat[]> GetChatsListAsync(Guid charId, CancellationToken cancellationToken)
    {
        var chats = _chatsCollection.Query().ToList();

        var result = chats
            .OrderByDescending(x => x.CreatedAt)
            .ToArray();

        return Task.FromResult(result);
    }
    
    public Task<Chat?> GetChatAsync(Guid id, CancellationToken cancellationToken)
    {
        var chat = _chatsCollection.FindOne(x => x.Id == id);
        return Task.FromResult<Chat?>(chat);
    }

    public Task SaveChatAsync(Chat chat)
    {
        _chatsCollection.Upsert(chat);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid charId)
    {
        _chatsCollection.DeleteMany(x => x.Id == charId);
        return Task.CompletedTask;
    }
}