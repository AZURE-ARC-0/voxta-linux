
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using LiteDB;

namespace Voxta.Data.LiteDB;

public class ChatLiteDBRepository : IChatRepository
{
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly ILiteCollection<Chat> _chatsCollection;

    public ChatLiteDBRepository(ILiteDatabase db, IChatMessageRepository chatMessageRepository)
    {
        _chatMessageRepository = chatMessageRepository;
        _chatsCollection = db.GetCollection<Chat>();
    }
    
    public Task<Chat[]> GetChatsListAsync(Guid charId, CancellationToken cancellationToken)
    {
        var chats = _chatsCollection.Query().ToList();

        var result = chats
            .Where(c => c.CharacterId == charId)
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

    public async Task DeleteAsync(Guid charId)
    {
        await _chatMessageRepository.DeleteChatMessages(charId);
        _chatsCollection.Delete(charId);
    }
}