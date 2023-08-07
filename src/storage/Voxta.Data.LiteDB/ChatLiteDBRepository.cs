
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using LiteDB;

namespace Voxta.Data.LiteDB;

public class ChatLiteDBRepository : IChatRepository
{
    private readonly ILiteCollection<Chat> _chatsCollection;
    private readonly ILiteCollection<ChatMessageData> _chatMessagesRepository;
    private readonly ILiteCollection<MemoryBook> _memoryBooksRepository;

    public ChatLiteDBRepository(ILiteDatabase db)
    {
        _chatsCollection = db.GetCollection<Chat>();
        _memoryBooksRepository = db.GetCollection<MemoryBook>();
        _chatMessagesRepository = db.GetCollection<ChatMessageData>();
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
    
    public Task<Chat?> GetChatByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var chat = _chatsCollection.FindOne(x => x.Id == id);
        return Task.FromResult<Chat?>(chat);
    }

    public Task SaveChatAsync(Chat chat)
    {
        _chatsCollection.Upsert(chat);
        return Task.CompletedTask;
    }

    public Task DeleteChatAsync(Guid chatId)
    {
        _chatMessagesRepository.DeleteMany(x => x.ChatId == chatId);
        _memoryBooksRepository.DeleteMany(x => x.ChatId == chatId);
        _chatsCollection.Delete(chatId);
        return Task.CompletedTask;
    }
}