using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IChatRepository
{
    Task<Chat[]> GetChatsListAsync(Guid charId, CancellationToken cancellationToken);
    Task<Chat?> GetChatAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChatAsync(Chat chat);
    Task DeleteAsync(Guid id);
}