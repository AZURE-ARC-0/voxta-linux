using Voxta.Abstractions.Diagnostics;

namespace Voxta.IntegrationTests.Shared;

public class TestPerformanceMetrics : IPerformanceMetrics
{
    public IPerformanceMetricsTracker Start(string key)
    {
        return new TestPerformanceMetricsTracker();
    }

    public IEnumerable<string> GetKeys()
    {
        return Array.Empty<string>();
    }

    public TimeSpan GetAverage(string key)
    {
        return TimeSpan.Zero;
    }

    public class TestPerformanceMetricsTracker : IPerformanceMetricsTracker
    {
        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Done()
        {
        }
    }
}