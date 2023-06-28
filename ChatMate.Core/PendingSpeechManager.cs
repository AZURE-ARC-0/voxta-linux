using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ChatMate.Abstractions.Model;

namespace ChatMate.Core;

public class PendingSpeechManager
{
    // TODO: Clean up old speech requests after some time
    // TODO: Instead of using a a pending technique, just load the chat?
    private readonly ConcurrentDictionary<string, SpeechRequest> _pendingSpeechRequests = new();

    public void Push(string id, SpeechRequest request)
    {
        _pendingSpeechRequests.TryAdd(id, request);
    }

    public bool TryGetValue(string id, [NotNullWhen(true)] out SpeechRequest? request)
    {
        return _pendingSpeechRequests.TryRemove(id, out request);
    }
}