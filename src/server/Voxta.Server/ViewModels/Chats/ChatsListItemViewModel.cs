using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels.Chats;

public class ChatsListItemViewModel
{
    public required Guid Id { get; init; }
    public required string Created { get; init; }
    public required ServerCharactersListLoadedMessage.CharactersListItem Character { get; init; }
}