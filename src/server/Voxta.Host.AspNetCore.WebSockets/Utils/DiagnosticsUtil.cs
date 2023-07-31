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
using Voxta.Abstractions.Exceptions;
using Voxta.Services.AzureSpeechService;
using Voxta.Services.Vosk;
#if(WINDOWS)
using Voxta.Services.WindowsSpeech;
#endif

namespace Voxta.Host.AspNetCore.WebSockets.Utils;

[Serializable]
public class DiagnosticsResult
{
    public required ServiceDiagnosticsResult Profile { get; init; }
    public required ServiceDiagnosticsResult[] TextGenServices { get; init; }
    public required ServiceDiagnosticsResult[] TextToSpeechServices { get; init; }
    public required ServiceDiagnosticsResult[] ActionInferenceServices { get; init; }
    public required ServiceDiagnosticsResult[] SpeechToTextServices { get; init; }
}

[Serializable]
public class ServiceDiagnosticsResult
{
    public required bool IsReady { get; init; }
    public required bool IsHealthy { get; init; }
    public required bool IsTested { get; init; }
    public required string ServiceName { get; init; }
    public required string Label { get; init; }
    public required string Status { get; init; }
    public string? Details { get; init; }
    public required string[] Features { get; init; }
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
        var profileResult = new ServiceDiagnosticsResult()
        {
            IsReady = true,
            IsHealthy = !string.IsNullOrEmpty(profile?.Name),
            ServiceName = "Profile",
            Label = "Profile and options",
            Status = profile?.Name ?? "No profile",
            IsTested = runTests,
            Features = Array.Empty<string>()
        };

        if (profile == null)
        {
            return new DiagnosticsResult
            {
                Profile = profileResult,
                ActionInferenceServices = Array.Empty<ServiceDiagnosticsResult>(),
                TextGenServices = Array.Empty<ServiceDiagnosticsResult>(),
                TextToSpeechServices = Array.Empty<ServiceDiagnosticsResult>(),
                SpeechToTextServices = Array.Empty<ServiceDiagnosticsResult>(),
            };
        }

        var textGen = new[]
        {
            Task.Run(async () => await TryTextGenAsync(OpenAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(NovelAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(KoboldAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextGenAsync(OobaboogaConstants.ServiceName, runTests, cancellationToken), cancellationToken),
        };
        
        var tts = new[]
        {
            Task.Run(async () => await TryTextToSpeechAsync(NovelAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextToSpeechAsync(ElevenLabsConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryTextToSpeechAsync(AzureSpeechServiceConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            #if(WINDOWS)
            Task.Run(async () => await TryTextToSpeechAsync(WindowsSpeechConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            #endif
        };
        
        var actionInference = new[]
        {
            Task.Run(async () => await TryActionInference(OpenAIConstants.ServiceName, runTests, cancellationToken), cancellationToken),
            Task.Run(async () => await TryActionInference(OobaboogaConstants.ServiceName, runTests, cancellationToken), cancellationToken)
        };

        var stt = new ServiceDiagnosticsResult[3];
        var sttTask = Task.Run(async () =>
        {
            stt[0] = await TrySpeechToText(VoskConstants.ServiceName, runTests, cancellationToken);
            stt[1] = await TrySpeechToText(AzureSpeechServiceConstants.ServiceName, runTests, cancellationToken);
            #if(WINDOWS)
            stt[2] = await TrySpeechToText(WindowsSpeechConstants.ServiceName, runTests, cancellationToken);
            #endif
        }, cancellationToken);

        await Task.WhenAll(textGen.Concat(tts).Concat(actionInference).Concat(new[] { sttTask }));

        return new DiagnosticsResult
        {
            Profile = profileResult,
            TextGenServices = textGen.Select(t => t.Result).OrderBy(x => profile?.TextGen.Order(x.ServiceName) ?? 0).ThenBy(x => x.ServiceName).ToArray(),
            TextToSpeechServices = tts.Select(t => t.Result).OrderBy(x => profile?.TextToSpeech.Order(x.ServiceName) ?? 0).ThenBy(x => x.ServiceName).ToArray(),
            ActionInferenceServices = actionInference.Select(t => t.Result).OrderBy(x => profile?.ActionInference.Order(x.ServiceName) ?? 0).ThenBy(x => x.ServiceName).ToArray(),
            SpeechToTextServices = stt.OrderBy(x => profile?.SpeechToText.Order(x.ServiceName) ?? 0).ThenBy(x => x.ServiceName).ToArray(),
        };
    }

    private async Task<ServiceDiagnosticsResult> TryTextGenAsync(string serviceName, bool runTests, CancellationToken cancellationToken)
    {

        return await TestServiceAsync(
            serviceName,
            runTests,
            async () => await _textGenFactory.CreateAsync(ServicesList.For(serviceName), serviceName, Array.Empty<string>(), "en-US", cancellationToken),
            async service =>
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
                        FirstMessage = "Beginning test.",
                    }
                }, cancellationToken);
                return "Response: " + result;
            }
        );
    }
    
    private async Task<ServiceDiagnosticsResult> TryTextToSpeechAsync(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            serviceName,
            runTests,
            async () => await _textToSpeechFactory.CreateAsync(ServicesList.For(serviceName), serviceName, Array.Empty<string>(), "en-US", cancellationToken),
            async service =>
            {
                var voices = await service.GetVoicesAsync(cancellationToken);
                var tunnel = new DeadSpeechTunnel();
                await service.GenerateSpeechAsync(new SpeechRequest
                {
                    Service = serviceName,
                    Text = "Hi",
                    Voice = voices.FirstOrDefault()?.Id ?? "default",
                    Culture = "en-US",
                    ContentType = service.ContentType,
                }, tunnel, cancellationToken);
                return tunnel.Result ?? "No Result";
            }
        );
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
    
    private async Task<ServiceDiagnosticsResult> TryActionInference(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            serviceName,
            runTests,
            async () => await _animationSelectionFactory.CreateAsync(ServicesList.For(serviceName), serviceName, Array.Empty<string>(), "en-US", cancellationToken),
            async service =>
            {
                var actions = new[] { "test_successful", "talk_to_user" };
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
                        FirstMessage = "Beginning test."
                    },
                    Actions = actions
                }, cancellationToken);
                return "Action: " + result;
            }
        );
    }
    
    private async Task<ServiceDiagnosticsResult> TrySpeechToText(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            serviceName,
            runTests,
            async () => await _speechToTextFactory.CreateAsync(ServicesList.For(serviceName), serviceName, Array.Empty<string>(), "en-US", cancellationToken),
            _ => Task.FromResult("Cannot be tested automatically")
        );
    }
    
    private static async Task<ServiceDiagnosticsResult> TestServiceAsync<TService>(string serviceName, bool runTests, Func<Task<TService>> createService, Func<TService, Task<string>> testService) where TService : IService
    {
        TService? service = default;
        try
        {
            string[]? features;
            try
            {
                service = await createService();
                features = service.Features;
            }
            catch (OperationCanceledException)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = serviceName,
                    Status = "Canceled",
                    Features = Array.Empty<string>()
                };
            }
            catch (ServiceDisabledException)
            {
                return new ServiceDiagnosticsResult
                {
                    IsReady = false,
                    IsHealthy = false,
                    IsTested = false,
                    ServiceName = serviceName,
                    Label = serviceName,
                    Status = "Disabled",
                    Features = Array.Empty<string>()
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
                    Label = serviceName,
                    Status = $"{exc.GetType().Namespace}: {exc.Message}",
                    Details = exc.ToString(),
                    Features = Array.Empty<string>()
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
                    Label = serviceName,
                    Status = "Configured",
                    Features = features
                };
            }

            try
            {
                var status = await testService(service);
                
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = true,
                    IsTested = true,
                    ServiceName = serviceName,
                    Label = serviceName,
                    Status = status,
                    Features = features
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
                    Label = serviceName,
                    Status = "Canceled",
                    Features = features
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
                    Label = serviceName,
                    Status = $"{exc.GetType().Name}: {exc.Message}",
                    Details = exc.ToString(),
                    Features = features
                };
            }
        }
        finally
        {
            service?.Dispose();
        }
    }
}
