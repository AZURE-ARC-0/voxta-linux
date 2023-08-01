
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
    
    public Task<ServerChatsListLoadedMessage.ChatsListItem[]> GetChatsListAsync(CancellationToken cancellationToken)
    {
        var chats = _chatsCollection.Query().ToList();

        var result = chats
            .Select(c => new ServerChatsListLoadedMessage.ChatsListItem
            {
                Id = c.Id ?? throw new NullReferenceException("Chat ID was null"),
                Name = c.Character.Name,
            })
            .OrderBy(x => x.Id)
            .ToArray();

        return Task.FromResult(result);
    }
    
    public Task<Chat?> GetChatAsync(string id, CancellationToken cancellationToken)
    {
        var chat = _chatsCollection.FindOne(x => x.Id == id);
        return Task.FromResult<Chat?>(chat);
    }

    public Task SaveChatAsync(Chat card)
    {
        _chatsCollection.Upsert(card);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string charId)
    {
        _chatsCollection.DeleteMany(x => x.Id == charId);
        return Task.CompletedTask;
    }
}