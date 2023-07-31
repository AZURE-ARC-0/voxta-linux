using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IChatRepository
{
    Task<ServerChatsListLoadedMessage.ChatsListItem[]> GetChatsListAsync(CancellationToken cancellationToken);
    Task<Chat?> GetChatAsync(string id, CancellationToken cancellationToken);
    Task SaveChatAsync(Chat card);
    Task DeleteAsync(string charId);
}