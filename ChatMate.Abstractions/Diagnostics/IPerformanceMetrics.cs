namespace ChatMate.Abstractions.Diagnostics;

public interface IPerformanceMetrics
{
    IPerformanceMetricsTracker Start(string key);
    IEnumerable<string> GetKeys();
    TimeSpan GetAverage(string key);
}

public interface IPerformanceMetricsTracker
{
    public void Pause();
    public void Resume();
    public void Done();
}