namespace Voxta.Abstractions.Services;

public interface IServiceObserver
{
    public void Record(string key, string value);
    public void Clear();
    public ServiceObserverRecord? GetRecord(string key);
    public IEnumerable<ServiceObserverRecord> GetRecords();
}

public static class ServiceObserverKeys
{
    public const string TextGenService = "TextGen.Service";
    public const string TextGenPrompt = "TextGen.Prompt";
    public const string TextGenResult = "TextGen.Result";
    public const string ActionInferenceService = "ActionInference.Service";
    public const string ActionInferencePrompt = "ActionInference.Prompt";
    public const string ActionInferenceResult = "ActionInference.Result";
}

public class ServiceObserverRecord
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}