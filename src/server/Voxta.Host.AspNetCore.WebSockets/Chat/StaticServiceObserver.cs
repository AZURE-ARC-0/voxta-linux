using Voxta.Abstractions.Services;

namespace Voxta.Host.AspNetCore.WebSockets;

public class StaticServiceObserver : IServiceObserver
{
    private readonly Dictionary<string, ServiceObserverRecord> _records = new();
    
    public void Record(string key, string value)
    {
        _records[key] = new ServiceObserverRecord
        {
            Key = key,
            Value = value,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public ServiceObserverRecord? GetRecord(string key)
    {
        return _records.TryGetValue(key, out var record) ? record : null;
    }

    public void Clear()
    {
        _records.Clear();
    }

    public IEnumerable<ServiceObserverRecord> GetRecords()
    {
        return _records.Values;
    }
}