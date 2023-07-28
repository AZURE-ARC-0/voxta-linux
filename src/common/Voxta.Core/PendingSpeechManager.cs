using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;

namespace Voxta.Core;

public class PendingSpeechManager
{
    private readonly ConcurrentDictionary<string, SpeechRequest> _pendingSpeechRequests = new();

    public void Push(string id, SpeechRequest request)
    {
        _pendingSpeechRequests.TryAdd(id, request);
    }

    public bool TryGetValue(string id, [NotNullWhen(true)] out SpeechRequest? request)
    {
        return _pendingSpeechRequests.TryGetValue(id, out request);
    }

    public void RemoveValue(string id)
    {
        _pendingSpeechRequests.Remove(id, out _);
    }
}
