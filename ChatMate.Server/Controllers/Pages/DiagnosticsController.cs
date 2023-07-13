using System.Runtime.ExceptionServices;
using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Server.ViewModels;
using ChatMate.Services.ElevenLabs;
using ChatMate.Services.KoboldAI;
using ChatMate.Services.NovelAI;
using ChatMate.Services.Oobabooga;
using ChatMate.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers.Pages;

[Controller]
public class DiagnosticsController : Controller
{
    private static readonly object Lock = new();
    private static readonly DiagnosticsViewModel AlreadyRunningVm = new()
    {
        Services = new List<DiagnosticsViewModel.ServiceStateViewModel>
        {
            new()
            {
                Name = "Diagnostics Error",
                IsHealthy = false,
                IsReady = false,
                Status = "Another diagnostic is still running. Wait and try again later."
            }
        }
    };

    private static bool _running;

    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IProfileRepository _profileRepository;
    private readonly IServiceFactory<ITextGenService> _textGenFactory;
    private readonly IServiceFactory<ITextToSpeechService> _textToSpeechFactory;
    private readonly IServiceFactory<IActionInferenceService> _animationSelectionFactory;

    public DiagnosticsController(
        IPerformanceMetrics performanceMetrics,
        IProfileRepository profileRepository,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<IActionInferenceService> animationSelectionFactory
        )
    {
        _performanceMetrics = performanceMetrics;
        _profileRepository = profileRepository;
        _textGenFactory = textGenFactory;
        _textToSpeechFactory = textToSpeechFactory;
        _animationSelectionFactory = animationSelectionFactory;
    }

    [HttpGet("/diagnostics")]
    public async Task<IActionResult> Diagnostics(CancellationToken cancellationToken)
    {
        var vm = await RunDiagnosticsAsync(false, cancellationToken);
        return View(vm);
    }

    [HttpPost("/diagnostics")]
    public async Task<IActionResult> Diagnostics([FromForm] string test, CancellationToken cancellationToken)
    {
        lock (Lock)
        {
            if (_running)
                return View(AlreadyRunningVm);

            _running = true;
        }

        try
        {
            var vm = await RunDiagnosticsAsync(true, cancellationToken);

            return View(vm);
        }
        finally
        {
            _running = false;
        }
    }

    private async Task<DiagnosticsViewModel> RunDiagnosticsAsync(bool runTests, CancellationToken cancellationToken)
    {
        var vm = new DiagnosticsViewModel();

        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        vm.Services = new List<DiagnosticsViewModel.ServiceStateViewModel>
        {
            new()
            {
                IsReady = true,
                IsHealthy = !string.IsNullOrEmpty(profile?.Name),
                Name = "ChatMate Profile",
                Status = profile?.Name ?? "No profile",
            }
        };

        var services = await Task.WhenAll(
            Task.Run(async () => await TryTextGenAsync(OpenAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(NovelAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(KoboldAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(OobaboogaConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextToSpeechAsync(NovelAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextToSpeechAsync(ElevenLabsConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryAnimSelect(OpenAIConstants.ServiceName, runTests, cancellationToken), cancellationToken)
        );
        vm.Services.AddRange(services.OrderBy(x => x.IsHealthy).ThenBy(x => x.IsReady).ThenBy(x => x.Name));

        vm.PerformanceMetrics = _performanceMetrics
            .GetKeys()
            .Select(k => new DiagnosticsViewModel.PerformanceMetricsViewModel { Key = k, Avg = _performanceMetrics.GetAverage(k) })
            .ToArray();
        return vm;
    }

    private async Task<DiagnosticsViewModel.ServiceStateViewModel> TryTextGenAsync(string key, bool runTests, CancellationToken cancellationToken)
    {
        var name = $"{key} (Text Gen)";
        
        ITextGenService service;
        try
        {
            service = await _textGenFactory.CreateAsync(key, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = false,
                IsHealthy = false,
                Name = name,
                Status = "Canceled"
            };
        }
        catch (Exception exc)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = false,
                IsHealthy = false,
                Name = name,
                Status = exc.Message
            };
        }
        
        if (!runTests)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = true,
                Name = name,
                Status = "Configuration OK (not tested)"
            };
        }
        
        try
        {
            var result = await service.GenerateReplyAsync(new ChatSessionData
            {
                UserName = "User",
                Character = new CharacterCard
                {
                    Name = "Assistant",
                    SystemPrompt = "You are a test assistant",
                    Description = "",
                    Personality = "",
                    Scenario = "This is a test",
                }
            }, cancellationToken);
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = true,
                Name = name,
                Status = "Response: " + result.Text
            };
        }
        catch (OperationCanceledException)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = false,
                Name = name,
                Status = "Canceled"
            };
        }
        catch (Exception exc)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = false,
                Name = name,
                Status = exc.ToString()
            };
        }
    }
    
    private async Task<DiagnosticsViewModel.ServiceStateViewModel> TryTextToSpeechAsync(string key, bool runTests, CancellationToken cancellationToken)
    {
        var name = $"{key} (TTS)";
        
        ITextToSpeechService service;
        try
        {
            service = await _textToSpeechFactory.CreateAsync(key, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = false,
                IsHealthy = false,
                Name = name,
                Status = "Canceled"
            };
        }
        catch (Exception exc)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = false,
                IsHealthy = false,
                Name = name,
                Status = exc.Message
            };
        }
        
        if (!runTests)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = true,
                Name = name,
                Status = "Configuration OK (not tested)"
            };
        }
        
        try
        {
            var voices = await service.GetVoicesAsync(cancellationToken);
            var tunnel = new DeadSpeechTunnel();
            await service.GenerateSpeechAsync(new SpeechRequest
            {
                Service = key,
                Text = "Hi",
                Voice = voices.FirstOrDefault()?.Id ?? "default",
                ContentType = service.ContentType,
            }, tunnel, cancellationToken);
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = tunnel.Result != null,
                Name = name,
                Status = tunnel.Result ?? "No result"
            };
        }
        catch (OperationCanceledException)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = false,
                Name = name,
                Status = "Canceled"
            };
        }
        catch (Exception exc)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = false,
                Name = name,
                Status = exc.ToString()
            };
        }
    }

    private class DeadSpeechTunnel : ISpeechTunnel
    {
        public string? Result { get; private set; }

        public Task ErrorAsync(Exception exc, CancellationToken cancellationToken)
        {
            ExceptionDispatchInfo.Capture(exc).Throw();
            throw exc;
        }

        public async Task SendAsync(AudioData audioData, CancellationToken cancellationToken)
        {
            Result = $"{audioData.Stream.Length} bytes, {audioData.ContentType}";
            await audioData.Stream.DisposeAsync();
        }
    }
    
    private async Task<DiagnosticsViewModel.ServiceStateViewModel> TryAnimSelect(string key, bool runTests, CancellationToken cancellationToken)
    {
        var name = $"{key} (Animation Selector)";
        
        IActionInferenceService service;
        try
        {
            service = await _animationSelectionFactory.CreateAsync(key, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = false,
                IsHealthy = false,
                Name = name,
                Status = "Canceled"
            };
        }
        catch (Exception exc)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = false,
                IsHealthy = false,
                Name = name,
                Status = exc.Message
            };
        }
        
        if (!runTests)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = true,
                Name = name,
                Status = "Configuration OK (not tested)"
            };
        }
        
        try
        {
            var result = await service.SelectActionAsync(new ChatSessionData
            {
                UserName = "User",
                Character = new CharacterCard
                {
                    Name = "Assistant",
                    SystemPrompt = "You are a test assistant",
                    Description = "",
                    Personality = "",
                    Scenario = "This is a test",
                }
            }, cancellationToken);
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = true,
                Name = name,
                Status = "Response: " + result
            };
        }
        catch (OperationCanceledException)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = false,
                Name = name,
                Status = "Canceled"
            };
        }
        catch (Exception exc)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsReady = true,
                IsHealthy = false,
                Name = name,
                Status = exc.ToString()
            };
        }
    }
}
