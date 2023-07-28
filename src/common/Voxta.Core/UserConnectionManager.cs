using System.Collections.Concurrent;

namespace Voxta.Core;

public interface IUserConnectionManager
{
    void Register(IUserConnection userConnection);
    void Unregister(IUserConnection userConnection);
    bool TryGetChatLock(IUserConnection connection);
    void ReleaseChatLock(IUserConnection connection);
}

public class UserConnectionManager : IUserConnectionManager
{
    private string? _activeChatLock;
    private readonly ConcurrentDictionary<string, IUserConnection> _connections = new();

    public void Register(IUserConnection connection)
    {
        _connections.TryAdd(connection.ConnectionId, connection);
    }
    
    public void Unregister(IUserConnection connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
        ReleaseChatLock(connection);
    }

    public bool TryGetChatLock(IUserConnection connection)
    {
        if (_activeChatLock != null && _activeChatLock != connection.ConnectionId)
            return false;
        
        _activeChatLock = connection.ConnectionId;
        return true;
    }

    public void ReleaseChatLock(IUserConnection connection)
    {
        if (_activeChatLock == connection.ConnectionId)
            _activeChatLock = null;
    }
}