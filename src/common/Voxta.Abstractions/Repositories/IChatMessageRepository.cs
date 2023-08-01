using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IChatMessageRepository
{
    Task<ChatMessageData[]> GetChatMessagesAsync(Guid chatId, CancellationToken cancellationToken);
    Task SaveMessageAsync(ChatMessageData message);
    Task DeleteChatMessages(Guid chatId);
}
