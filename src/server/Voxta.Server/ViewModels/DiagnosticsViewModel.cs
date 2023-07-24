namespace Voxta.Server.ViewModels;

public class DiagnosticsViewModel
{
    public PerformanceMetricsViewModel[] PerformanceMetrics { get; set; } = Array.Empty<PerformanceMetricsViewModel>();

    public class PerformanceMetricsViewModel
    {
        public required string Key { get; init; }
        public TimeSpan Avg { get; init; }
    }
}