using Voxta.Abstractions.Model;

namespace Voxta.Core.Tests;

public static class ChatSessionDataExtensions
{
    public static string GetMessagesAsString(this IChatInferenceData chat)
    {
        using var token = chat.GetReadToken();
        return string.Join("\n", token.Messages.Select(m => $"{m.User}: {m.Value}"));
    }
}
