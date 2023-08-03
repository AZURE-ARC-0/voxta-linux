namespace Voxta.Abstractions.Services;

public interface IServiceObserver
{
    public void Record(string key, string value);
    public void Clear();
    public IEnumerable<ServiceObserverRecord> GetRecords();
}

public class ServiceObserverRecord
{
    public string Key { get; init; }
    public string Value { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}