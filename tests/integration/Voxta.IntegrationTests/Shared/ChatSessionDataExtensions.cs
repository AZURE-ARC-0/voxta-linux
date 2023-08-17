using Voxta.Abstractions.Model;

namespace Voxta.IntegrationTests.Shared;

public static class ChatSessionDataExtensions
{
    public static IReadOnlyList<ChatMessageData> GetAllMessages(this IChatInferenceData chat)
    {
        using var token = chat.GetReadToken();
        return token.Messages.ToArray();
    }
}