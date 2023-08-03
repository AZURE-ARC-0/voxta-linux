using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels;

public class ChatsListItemViewModel
{
    public required Guid Id { get; init; }
    public required string Created { get; init; }
    public required ServerCharactersListLoadedMessage.CharactersListItem Character { get; init; }
}