using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ChatMate.Server;

public class PendingSpeechManager
{
    // TODO: Clean up old speech requests after some time
    // TODO: Instead of using a a pending technique, just load the chat?
    private readonly ConcurrentDictionary<string, SpeechRequest> _pendingSpeechRequests = new();

    public void Push(Guid chatId, Guid messageId, SpeechRequest request)
    {
        _pendingSpeechRequests.TryAdd($"{chatId}/{messageId}", request);
    }

    public bool TryGetValue(Guid chatId, Guid messageId, [NotNullWhen(true)] out SpeechRequest? request)
    {
        return _pendingSpeechRequests.TryRemove($"{chatId}/{messageId}", out request);
    }
}

public class SpeechRequest
{
    public required string Service { get; init; }
    public required string Text { get; init; }
    public required string Voice { get; set; }
}