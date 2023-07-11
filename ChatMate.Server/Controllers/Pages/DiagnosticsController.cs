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
using ChatMate.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers.Pages;

[Controller]
public class DiagnosticsController : Controller
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IProfileRepository _profileRepository;
    private readonly IServiceFactory<ITextGenService> _textGenFactory;
    private readonly IServiceFactory<ITextToSpeechService> _textToSpeechFactory;
    private readonly IServiceFactory<IAnimationSelectionService> _animationSelectionFactory;

    public DiagnosticsController(
        IPerformanceMetrics performanceMetrics,
        IProfileRepository profileRepository,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<IAnimationSelectionService> animationSelectionFactory
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
        var vm = new DiagnosticsViewModel();

        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        vm.Services.Add(new DiagnosticsViewModel.ServiceStateViewModel { Name = "ChatMate Profile", Status = profile?.Name ?? "No profile", IsHealthy = !string.IsNullOrEmpty(profile?.Name) });
        var services = await Task.WhenAll(
            Task.Run(async () => await TryTextGenAsync(OpenAIConstants.ServiceName, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(NovelAIConstants.ServiceName, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(KoboldAIConstants.ServiceName, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextToSpeechAsync(NovelAIConstants.ServiceName, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextToSpeechAsync(ElevenLabsConstants.ServiceName, cancellationToken), cancellationToken),
            Task.Run(async () => await TryAnimSelect(OpenAIConstants.ServiceName, cancellationToken), cancellationToken)
        );
        vm.Services.AddRange(services);

        vm.PerformanceMetrics = _performanceMetrics
            .GetKeys()
            .Select(k => new DiagnosticsViewModel.PerformanceMetricsViewModel { Key = k, Avg = _performanceMetrics.GetAverage(k) })
            .ToArray();
        
        return View(vm);
    }

    private async Task<DiagnosticsViewModel.ServiceStateViewModel> TryTextGenAsync(string key, CancellationToken cancellationToken)
    {
        var name = $"{key} (Text Gen)";
        try
        {
            var service = await _textGenFactory.CreateAsync(key, cancellationToken);
            var result = await service.GenerateReplyAsync(new ChatSessionData
            {
                Preamble = new TextData { Text = "You are a nice chat bot." },
                BotName = "Text Bot",
                UserName = "Test User",
            }, cancellationToken);
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsHealthy = true,
                Name = name,
                Status = "Response: " + result.Text
            };
        }
        catch (OperationCanceledException)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsHealthy = false,
                Name = name,
                Status = "Canceled"
            };
        }
        catch (Exception exc)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsHealthy = false,
                Name = name,
                Status = exc.ToString()
            };
        }
    }
    
    private async Task<DiagnosticsViewModel.ServiceStateViewModel> TryTextToSpeechAsync(string key, CancellationToken cancellationToken)
    {
        var name = $"{key} (TTS)";
        try
        {
            var service = await _textToSpeechFactory.CreateAsync(key, cancellationToken);
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
                IsHealthy = tunnel.Result != null,
                Name = name,
                Status = tunnel.Result ?? "No result"
            };
        }
        catch (OperationCanceledException)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsHealthy = false,
                Name = name,
                Status = "Canceled"
            };
        }
        catch (Exception exc)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsHealthy = false,
                Name = name,
                Status = exc.ToString()
            };
        }
    }

    private class DeadSpeechTunnel : ISpeechTunnel
    {
        public string? Result { get; private set; }

        public Task ErrorAsync(string message, CancellationToken cancellationToken)
        {
            throw new Exception(message);
        }

        public async Task SendAsync(AudioData audioData, CancellationToken cancellationToken)
        {
            Result = $"{audioData.Stream.Length} bytes, {audioData.ContentType}";
            await audioData.Stream.DisposeAsync();
        }
    }
    
    private async Task<DiagnosticsViewModel.ServiceStateViewModel> TryAnimSelect(string key, CancellationToken cancellationToken)
    {
        var name = $"{key} (Animation Selector)";
        try
        {
            var service = await _animationSelectionFactory.CreateAsync(key, cancellationToken);
            var result = await service.SelectAnimationAsync(new ChatSessionData
            {
                Preamble = new TextData { Text = "You are a nice chat bot." },
                BotName = "Text Bot",
                UserName = "Test User",
            }, cancellationToken);
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsHealthy = true,
                Name = name,
                Status = "Response: " + result
            };
        }
        catch (OperationCanceledException)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsHealthy = false,
                Name = name,
                Status = "Canceled"
            };
        }
        catch (Exception exc)
        {
            return new DiagnosticsViewModel.ServiceStateViewModel
            {
                IsHealthy = false,
                Name = name,
                Status = exc.ToString()
            };
        }
    }
}
