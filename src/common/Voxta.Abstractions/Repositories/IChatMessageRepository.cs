using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IChatMessageRepository
{
    Task<ChatMessageData[]> GetChatMessagesAsync(Guid chatId, bool includeSummarizedMessages, CancellationToken cancellationToken);
    Task<ChatMessageData?> GetChatMessageAsync(Guid messageId, CancellationToken cancellationToken);
    Task SaveMessageAsync(ChatMessageData message);
    Task UpdateMessageAsync(ChatMessageData message);
    Task DeleteChatMessages(Guid chatId);
    Task DeleteMessageAsync(Guid messageId);
}
