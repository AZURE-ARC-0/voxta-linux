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
    public IActionResult Performance()
    {
        var vm = new DiagnosticsViewModel
        {
            PerformanceMetrics = _performanceMetrics
                .GetKeys()
                .Select(k => new DiagnosticsViewModel.PerformanceMetricsViewModel { Key = k, Avg = _performanceMetrics.GetAverage(k) })
                .ToArray()
        };
        return View(vm);
    }
}
