using Voxta.Abstractions.Diagnostics;
using Voxta.Server.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Voxta.Server.Controllers;

[Controller]
public class DiagnosticsController : Controller
{
    private readonly IPerformanceMetrics _performanceMetrics;

    public DiagnosticsController(
        IPerformanceMetrics performanceMetrics
    )
    {
        _performanceMetrics = performanceMetrics;
    }

    [HttpGet("/diagnostics")]
    public Task<IActionResult> Diagnostics(CancellationToken cancellationToken)
    {
        var vm = new DiagnosticsViewModel
        {
            PerformanceMetrics = _performanceMetrics
                .GetKeys()
                .Select(k => new DiagnosticsViewModel.PerformanceMetricsViewModel { Key = k, Avg = _performanceMetrics.GetAverage(k) })
                .ToArray()
        };
        return Task.FromResult<IActionResult>(View(vm));
    }
}
