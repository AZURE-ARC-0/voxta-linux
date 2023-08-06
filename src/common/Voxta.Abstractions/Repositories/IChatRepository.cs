using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IChatRepository
{
    Task<Chat[]> GetChatsListAsync(Guid charId, CancellationToken cancellationToken);
    Task<Chat?> GetChatByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChatAsync(Chat chat);
    Task DeleteChatAsync(Guid chatId);
}