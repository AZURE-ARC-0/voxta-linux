using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels;

public class ChatViewModel
{
    public required Guid Id { get; init; }
    public required string Created { get; init; }
    public required Character Character { get; init; }
    public required ChatMessageData[] Messages { get; init; }
}