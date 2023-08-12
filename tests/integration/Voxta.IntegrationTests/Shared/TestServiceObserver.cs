using Voxta.Abstractions.Services;

namespace Voxta.IntegrationTests.Shared;

public class TestServiceObserver : IServiceObserver
{
    private readonly Dictionary<string, string> _records = new();
    
    public void Record(string key, string value)
    {
        _records[key] = value;
    }

    public void Clear()
    {
        _records.Clear();
    }

    public ServiceObserverRecord? GetRecord(string key)
    {
        return _records.TryGetValue(key, out var value) ? new ServiceObserverRecord { Key = key, Value = value } : null;
    }

    public IEnumerable<ServiceObserverRecord> GetRecords()
    {
        return Array.Empty<ServiceObserverRecord>();
    }
}