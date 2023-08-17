using Voxta.Abstractions.Services;

namespace Voxta.Host.AspNetCore.WebSockets;

public class StaticServiceObserver : IServiceObserver
{
    private readonly object _lock = new();
    private readonly Dictionary<string, ServiceObserverRecord> _records = new();
    
    public void Record(string key, string value)
    {
        lock (_lock)
        {
            _records[key] = new ServiceObserverRecord
            {
                Key = key,
                Value = value,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public ServiceObserverRecord? GetRecord(string key)
    {
        lock (_lock)
        {
            return _records.TryGetValue(key, out var record) ? record : null;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _records.Clear();
        }
    }

    public IEnumerable<ServiceObserverRecord> GetRecords()
    {
        lock (_lock)
        {
            return _records.Values.ToArray();
        }
    }
}