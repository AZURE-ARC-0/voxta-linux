using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels.Chats;

[Serializable]
public class ChatsListItemViewModel
{
    public required DateTimeOffset CreatedDateTimeOffset { get; init; }
    public required Guid Id { get; init; }
    public required string Created { get; init; }
    public required ServerCharactersListLoadedMessage.CharactersListItem Character { get; init; }
}