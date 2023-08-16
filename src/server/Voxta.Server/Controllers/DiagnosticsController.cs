using Voxta.Abstractions.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Services;
using Voxta.Server.ViewModels.Diagnostics;

namespace Voxta.Server.Controllers;

[Controller]
public class DiagnosticsController : Controller
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public DiagnosticsController(
        IPerformanceMetrics performanceMetrics,
        IServiceObserver serviceObserver
    )
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    [HttpGet("/diagnostics")]
    public IActionResult Diagnostics()
    {
        var vm = new DiagnosticsViewModel
        {
            PerformanceMetrics = _performanceMetrics
                .GetKeys()
                .Select(k => new DiagnosticsViewModel.PerformanceMetricsViewModel { Key = k, Avg = _performanceMetrics.GetAverage(k) })
                .ToArray(),
            Records = _serviceObserver.GetRecords()
                .OrderByDescending(r => r.Timestamp)
                .ToArray(),
        };
        return View(vm);
    }

    #warning BRING THIS BACK
    [HttpPost("/diagnostics/test")]
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
    public async Task<IActionResult> TestAllSettings([FromForm] bool test, CancellationToken cancellationToken)
    {
        if (!test) throw new InvalidOperationException("Unexpected settings test without test flag.");

        throw new NotImplementedException();
        //
        // var vm = await GetSettingsViewModel(() => _diagnosticsUtil.TestAllServicesAsync(cancellationToken), cancellationToken);
        //  return View("Settings", vm);
    }
}
