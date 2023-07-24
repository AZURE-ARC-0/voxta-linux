using System.Runtime.ExceptionServices;
using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Services.ElevenLabs;
using Voxta.Services.KoboldAI;
using Voxta.Services.NovelAI;
using Voxta.Services.Oobabooga;
using Voxta.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Voxta.Services.AzureSpeechService;
using Voxta.Services.Vosk;

namespace Voxta.Host.AspNetCore.WebSockets.Utils;

public class DiagnosticsResult
{
    public required List<ServiceDiagnosticsResult>? Services { get; init; }
}

public class ServiceDiagnosticsResult
{
    public bool IsReady { get; init; }
    public bool IsHealthy { get; init; }
    public bool IsTested { get; init; }
    public required string ServiceName { get; init; }
    public required string Label { get; init; }
    public required string Status { get; init; }
    public string? Details { get; init; }
}

public class DiagnosticsUtil
{
    private static readonly object Lock = new();

    private static bool _running;

    private readonly IProfileRepository _profileRepository;
    private readonly IServiceFactory<ITextGenService> _textGenFactory;
    private readonly IServiceFactory<ITextToSpeechService> _textToSpeechFactory;
    private readonly IServiceFactory<ISpeechToTextService> _speechToTextFactory;
    private readonly IServiceFactory<IActionInferenceService> _animationSelectionFactory;

    public DiagnosticsUtil(
        IProfileRepository profileRepository,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<ISpeechToTextService> speechToTextFactory,
        IServiceFactory<IActionInferenceService> animationSelectionFactory
        )
    {
        _profileRepository = profileRepository;
        _textGenFactory = textGenFactory;
        _textToSpeechFactory = textToSpeechFactory;
        _speechToTextFactory = speechToTextFactory;
        _animationSelectionFactory = animationSelectionFactory;
    }

    public Task<DiagnosticsResult> GetAllServicesAsync(CancellationToken cancellationToken)
    {
        return TestAllServicesAsync(false, cancellationToken);
    }

    [HttpPost("/diagnostics")]
    public async Task<DiagnosticsResult> TestAllServicesAsync(CancellationToken cancellationToken)
    {
        lock (Lock)
        {
            if (_running)
                throw new InvalidOperationException("Another diagnostic is still running. Wait and try again later.");

            _running = true;
        }

        try
        {
            return await TestAllServicesAsync(true, cancellationToken);
        }
        finally
        {
            _running = false;
        }
    }

    private async Task<DiagnosticsResult> TestAllServicesAsync(bool runTests, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        var result = new DiagnosticsResult
        {
            Services = new List<ServiceDiagnosticsResult>
            {
                new()
                {
                    IsReady = true,
                    IsHealthy = !string.IsNullOrEmpty(profile?.Name),
                    ServiceName = "Profile",
                    Label = "Profile",
                    Status = profile?.Name ?? "No profile",
                }
            }
        };

        var services = await Task.WhenAll(
            Task.Run(async () => await TryTextGenAsync(OpenAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(NovelAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(KoboldAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(OobaboogaConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextToSpeechAsync(NovelAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextToSpeechAsync(ElevenLabsConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextToSpeechAsync(AzureSpeechServiceConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryAnimSelect(OpenAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TrySpeechToText(VoskConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TrySpeechToText(AzureSpeechServiceConstants.ServiceName, runTests, cancellationToken), cancellationToken)
        );
        result.Services.AddRange(services.OrderBy(x => x.IsHealthy).ThenBy(x => x.IsReady).ThenBy(x => x.Label));
        return result;
    }

    private async Task<ServiceDiagnosticsResult> TryTextGenAsync(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        var name = $"{serviceName} (Text Gen)";
        
        ITextGenService? service = null;
        try
        {
            try
            {
                service = await _textGenFactory.CreateAsync(serviceName, "en-US", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Canceled"
                };
            }
            catch (Exception exc)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = $"{exc.GetType().Namespace}: {exc.Message}",
                    Details = exc.ToString()
                };
            }

            if (!runTests)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = true,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Configured"
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
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = true,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Response: " + result.Text
                };
            }
            catch (OperationCanceledException)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = false,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Canceled"
                };
            }
            catch (Exception exc)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = false,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = name,
                    Status = $"{exc.GetType().Name}: {exc.Message}",
                    Details = exc.ToString()
                };
            }
        }
        finally
        {
            service?.Dispose();
        }
    }
    
    private async Task<ServiceDiagnosticsResult> TryTextToSpeechAsync(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        var name = $"{serviceName} (TTS)";
        
        ITextToSpeechService? service = null;
        try
        {
            try
            {
                service = await _textToSpeechFactory.CreateAsync(serviceName, "en-US", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Canceled"
                };
            }
            catch (Exception exc)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = $"{exc.GetType().Namespace}: {exc.Message}",
                    Details = exc.ToString(),
                };
            }

            if (!runTests)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = true,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Configured"
                };
            }

            try
            {
                var voices = await service.GetVoicesAsync(cancellationToken);
                var tunnel = new DeadSpeechTunnel();
                await service.GenerateSpeechAsync(new SpeechRequest
                {
                    Service = serviceName,
                    Text = "Hi",
                    Voice = voices.FirstOrDefault()?.Id ?? "default",
                    ContentType = service.ContentType,
                }, tunnel, cancellationToken);
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = tunnel.Result != null,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = name,
                    Status = tunnel.Result ?? "No result"
                };
            }
            catch (OperationCanceledException)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = false,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Canceled"
                };
            }
            catch (Exception exc)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = false,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = name,
                    Status = $"{exc.GetType().Namespace}: {exc.Message}",
                    Details = exc.ToString(),
                };
            }
        }
        finally
        {
            service?.Dispose();
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
    
    private async Task<ServiceDiagnosticsResult> TryAnimSelect(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        var name = $"{serviceName} (Animation Selector)";
        
        IActionInferenceService? service = null;
        try
        {
            try
            {
                service = await _animationSelectionFactory.CreateAsync(serviceName, "en-US", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Canceled"
                };
            }
            catch (Exception exc)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = $"{exc.GetType().Namespace}: {exc.Message}",
                    Details = exc.ToString(),
                };
            }

            if (!runTests)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = true,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Configured"
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
                    },
                    Actions = new[] { "test_successful", "talk_to_user" }
                }, cancellationToken);
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = true,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Response: " + result
                };
            }
            catch (OperationCanceledException)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = false,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Canceled"
                };
            }
            catch (Exception exc)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = false,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = name,
                    Status = $"{exc.GetType().Namespace}: {exc.Message}",
                    Details = exc.ToString()
                };
            }
        }
        finally
        {
            service?.Dispose();
        }
    }
    
    private async Task<ServiceDiagnosticsResult> TrySpeechToText(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        var name = $"{serviceName} (Speech To Text)";

        ISpeechToTextService? service = null;
        try
        {
            try
            {
                service = await _speechToTextFactory.CreateAsync(serviceName, "en-US", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Canceled"
                };
            }
            catch (Exception exc)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = $"{exc.GetType().Namespace}: {exc.Message}",
                    Details = exc.ToString(),
                };
            }

            if (!runTests)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = true,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = name,
                    Status = "Configured"
                };
            }

            return new ServiceDiagnosticsResult
            {
                IsReady = true,
                IsHealthy = true,
                IsTested = true,
                ServiceName = serviceName,
                Label = name,
                Status = "Cannot be tested automatically"
            };
        }
        finally
        {
            service?.Dispose();
        }
    }
}
