namespace ChatMate.Abstractions.Diagnostics;

public interface IPerformanceMetrics
{
    IPerformanceMetricsTracker Start(string key);
    void Track(string key, TimeSpan elapsed);
    ICollection<string> GetKeys();
    TimeSpan GetAverage(string key);
}

public interface IPerformanceMetricsTracker
{
    public void Pause();
    public void Resume();
    public void Done();
}