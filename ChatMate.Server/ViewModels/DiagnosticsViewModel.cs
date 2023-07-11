namespace ChatMate.Server.ViewModels;

public class DiagnosticsViewModel
{
    public PerformanceMetricsViewModel[] PerformanceMetrics { get; set; } = Array.Empty<PerformanceMetricsViewModel>();
    public List<ServiceStateViewModel> Services { get; } = new();

    public class ServiceStateViewModel
    {
        public bool IsHealthy { get; init; }
        public required string Name { get; init; }
        public required string Status { get; init; }
    }

    public class PerformanceMetricsViewModel
    {
        public required string Key { get; init; }
        public TimeSpan Avg { get; init; }
    }
}