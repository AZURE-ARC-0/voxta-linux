using Voxta.Abstractions.Services;

namespace Voxta.Server.ViewModels.Diagnostics;

public class DiagnosticsViewModel
{
    public required PerformanceMetricsViewModel[] PerformanceMetrics { get; init; }
    public required ServiceObserverRecord[] Records { get; init; }

    public class PerformanceMetricsViewModel
    {
        public required string Key { get; init; }
        public TimeSpan Avg { get; init; }
    }
}