using System.Collections.Concurrent;
using System.Diagnostics;
using ChatMate.Abstractions.Diagnostics;

namespace ChatMate.Host.AspNetCore.WebSockets;

public class StaticPerformanceMetrics : IPerformanceMetrics
{
    private struct Counter
    {
        public TimeSpan Elapsed { get; init; }
        public int Count { get; init; }
    }
    
    private readonly ConcurrentDictionary<string, Counter> _counters = new();

    public IPerformanceMetricsTracker Start(string key)
    {
        return new StaticPerformanceMetricsTracker(this, key);
    }

    public void Track(string key, TimeSpan elapsed)
    {
        _counters.AddOrUpdate(key, new Counter { Elapsed = elapsed, Count = 1 }, (_, c) => new Counter { Elapsed = c.Elapsed + elapsed, Count = c.Count + 1 });
    }

    public IEnumerable<string> GetKeys()
    {
        return _counters.Keys;
    }

    public TimeSpan GetAverage(string key)
    {
        return _counters.TryGetValue(key, out var counter)
            ? TimeSpan.FromTicks(counter.Elapsed.Ticks / counter.Count)
            : TimeSpan.Zero;
    }
}

public class StaticPerformanceMetricsTracker : IPerformanceMetricsTracker
{
    private readonly StaticPerformanceMetrics _metrics;
    private readonly string _key;
    private readonly Stopwatch _sw;

    public StaticPerformanceMetricsTracker(StaticPerformanceMetrics metrics, string key)
    {
        _metrics = metrics;
        _key = key;
        _sw = Stopwatch.StartNew();
    }

    public void Pause()
    {
        _sw.Stop();
    }

    public void Resume()
    {
        _sw.Start();
    }

    public void Done()
    {
        _sw.Stop();
        _metrics.Track(_key, _sw.Elapsed);
    }
}