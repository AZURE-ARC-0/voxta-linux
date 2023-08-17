using Voxta.Abstractions.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Services;
using Voxta.Host.AspNetCore.WebSockets.Utils;
using Voxta.Server.ViewModels.Diagnostics;

namespace Voxta.Server.Controllers;

[Controller]
public class DiagnosticsController : Controller
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;
    private readonly DiagnosticsUtil _diagnosticsUtil;

    public DiagnosticsController(
        IPerformanceMetrics performanceMetrics,
        IServiceObserver serviceObserver, DiagnosticsUtil diagnosticsUtil)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
        _diagnosticsUtil = diagnosticsUtil;
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

    [HttpPost("/diagnostics/test")]
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
    public async Task<IActionResult> Test([FromForm] bool test, CancellationToken cancellationToken)
    {
        if (!test) throw new InvalidOperationException("Unexpected settings test without test flag.");
        var services = await _diagnosticsUtil.TestAllServicesAsync(cancellationToken);
        var vm = new TestViewModel
        {
            Services = new SettingsServiceViewModel[]
            {
                new()
                {
                    Name = "stt",
                    Title = "Speech To Text Services",
                    Services = services.SpeechToTextServices,
                },
                new()
                {
                    Name = "textgen",
                    Title = "Text Generation Services",
                    Services = services.TextGenServices,
                },
                new()
                {
                    Name = "tts",
                    Title = "Text To Speech Services",
                    Services = services.TextToSpeechServices,
                },
                new()
                {
                    Name = "action_inference",
                    Title = "Action Inference Services",
                    Services = services.ActionInferenceServices,
                },
                new()
                {
                    Name = "summarization",
                    Title = "Summarization Services",
                    Services = services.SummarizationServices,
                },
            }
        };
        return View(vm);
    }
}
