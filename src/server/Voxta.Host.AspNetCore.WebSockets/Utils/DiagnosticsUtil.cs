using System.Runtime.ExceptionServices;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Exceptions;

namespace Voxta.Host.AspNetCore.WebSockets.Utils;

[Serializable]
public class DiagnosticsResult
{
    public required ServiceDiagnosticsResult[] TextGenServices { get; init; }
    public required ServiceDiagnosticsResult[] TextToSpeechServices { get; init; }
    public required ServiceDiagnosticsResult[] ActionInferenceServices { get; init; }
    public required ServiceDiagnosticsResult[] SpeechToTextServices { get; init; }
    public required ServiceDiagnosticsResult[] SummarizationServices { get; init; }
}

[Serializable]
public class ServiceDiagnosticsResult
{
    public required bool IsReady { get; init; }
    public required bool IsHealthy { get; init; }
    public required bool IsTested { get; init; }
    public required string ServiceName { get; init; }
    public required Guid ServiceId { get; set; }
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
    private readonly IServiceFactory<IActionInferenceService> _actionInferenceFactory;
    private readonly IServiceFactory<ISummarizationService> _summarizationFactory;
    private readonly IServicesRepository _servicesRepository;
    private readonly IServiceDefinitionsRegistry _serviceDefinitions;

    public DiagnosticsUtil(
        IProfileRepository profileRepository,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<ISpeechToTextService> speechToTextFactory,
        IServiceFactory<IActionInferenceService> actionInferenceFactory,
        IServiceFactory<ISummarizationService> summarizationFactory, IServicesRepository servicesRepository, IServiceDefinitionsRegistry serviceDefinitions)
    {
        _profileRepository = profileRepository;
        _textGenFactory = textGenFactory;
        _textToSpeechFactory = textToSpeechFactory;
        _speechToTextFactory = speechToTextFactory;
        _actionInferenceFactory = actionInferenceFactory;
        _summarizationFactory = summarizationFactory;
        _servicesRepository = servicesRepository;
        _serviceDefinitions = serviceDefinitions;
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
        var profile = await _profileRepository.GetRequiredProfileAsync(cancellationToken);

        var allServices = await _servicesRepository.GetServicesAsync(cancellationToken);
        var allServicesDefs = allServices.Select(x => (Link: x, Def: _serviceDefinitions.Get(x.ServiceName))).ToArray();

        var textGen = allServicesDefs
            .Where(s => s.Def.TextGen.IsSupported())
            .Select(s => Task.Run(async () => await TryTextGenAsync(s.Link, runTests, cancellationToken), cancellationToken))
            .ToArray();
        
        var tts = allServicesDefs
            .Where(s => s.Def.TTS.IsSupported())
            .Select(s => Task.Run(async () => await TryTextToSpeechAsync(s.Link, runTests, cancellationToken), cancellationToken))
            .ToArray();
        
        var actionInference = allServicesDefs
            .Where(s => s.Def.ActionInference.IsSupported())
            .Select(s => Task.Run(async () => await TryActionInferenceAsync(s.Link, runTests, cancellationToken), cancellationToken))
            .ToArray();
        
        var summarization = allServicesDefs
            .Where(s => s.Def.Summarization.IsSupported())
            .Select(s => Task.Run(async () => await TrySummarizationAsync(s.Link, runTests, cancellationToken), cancellationToken))
            .ToArray();

        var sttNames = allServicesDefs
            .Where(s => s.Def.STT.IsSupported())
            .ToArray();
        var stt = new ServiceDiagnosticsResult[sttNames.Length];
        var sttTask = Task.Run(async () =>
        {
            for (var i = 0; i < sttNames.Length; i++)
            {
                stt[i] = await TrySpeechToText(sttNames[i].Link, runTests, cancellationToken);
            }
        }, cancellationToken);

        await Task.WhenAll(textGen.Concat(tts).Concat(actionInference).Concat(summarization).Concat(new[] { sttTask }));

        return new DiagnosticsResult
        {
            TextGenServices = textGen.Select(t => t.Result).OrderBy(x => profile.TextGen.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
            TextToSpeechServices = tts.Select(t => t.Result).OrderBy(x => profile.TextToSpeech.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
            ActionInferenceServices = actionInference.Select(t => t.Result).OrderBy(x => profile.ActionInference.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
            SummarizationServices = summarization.Select(t => t.Result).OrderBy(x => profile.Summarization.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
            SpeechToTextServices = stt.OrderBy(x => profile.SpeechToText.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
        };
    }

    private async Task<ServiceDiagnosticsResult> TryTextGenAsync(ConfiguredService s, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            s,
            runTests,
            async () => await _textGenFactory.CreateSpecificAsync(new ServiceLink(s), "en-US", !runTests, cancellationToken),
            async service =>
            {
                var chat = new ChatSessionData
                {
                    Culture = "en-US",
                    User = new ChatSessionDataUser
                    {
                        Name = "User"
                    },
                    Chat = new Chat { Id = Guid.Empty, CharacterId = Guid.Empty },
                    Character = new ChatSessionDataCharacter
                    {
                        Name = "Assistant",
                        SystemPrompt = "You are a test assistant and must comply with user instructions.",
                        Description = "",
                        Personality = "",
                        Scenario = "This is a test.",
                        FirstMessage = "Please specify your test request.",
                    }
                };
                chat.AddMessage(chat.Character, chat.Character.FirstMessage);
                chat.AddMessage(chat.User, "I need to test if you are working correctly. You must answer with the word 'success'.");
                var result = await service.GenerateReplyAsync(chat, cancellationToken);
                return ("Response: " + result, result.Contains("success", StringComparison.InvariantCultureIgnoreCase));
            }
        );
    }
    
    private async Task<ServiceDiagnosticsResult> TryTextToSpeechAsync(ConfiguredService s, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            s,
            runTests,
            async () => await _textToSpeechFactory.CreateSpecificAsync(new ServiceLink(s), "en-US", !runTests, cancellationToken),
            async service =>
            {
                var voices = await service.GetVoicesAsync(cancellationToken);
                var tunnel = new DeadSpeechTunnel();
                await service.GenerateSpeechAsync(new SpeechRequest
                {
                    ServiceName = s.ServiceName,
                    ServiceId = s.Id,
                    Text = "Hi",
                    Voice = voices.FirstOrDefault()?.Id ?? "default",
                    Culture = "en-US",
                    ContentType = service.ContentType,
                }, tunnel, cancellationToken);
                return (tunnel.Result ?? "No Result", tunnel.Result != null);
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
    
    private async Task<ServiceDiagnosticsResult> TryActionInferenceAsync(ConfiguredService s, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            s,
            runTests,
            async () => await _actionInferenceFactory.CreateSpecificAsync(new ServiceLink(s), "en-US", !runTests, cancellationToken),
            async service =>
            {
                var actions = new[] { "test_successful", "test_failed" };
                var chat = new ChatSessionData
                {
                    Culture = "en-US",
                    User = new ChatSessionDataUser { Name = "User" },
                    Chat = new Chat { Id = Guid.Empty, CharacterId = Guid.Empty },
                    Character = new ChatSessionDataCharacter
                    {
                        Name = "Assistant",
                        SystemPrompt = "You are a test assistant and must comply with user requests.",
                        Description = "",
                        Personality = "",
                        Scenario = "This is a test",
                        FirstMessage = "Ready.",
                    },
                    Actions = actions
                };
                chat.AddMessage(chat.Character, "Nice, looks like everything is working fine!");
                var result = await service.SelectActionAsync(chat, cancellationToken);
                return ("Action: " + result, result == "test_successful");
            }
        );
    }
    
    private async Task<ServiceDiagnosticsResult> TrySummarizationAsync(ConfiguredService s, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            s,
            runTests,
            async () => await _summarizationFactory.CreateSpecificAsync(new ServiceLink(s), "en-US", !runTests, cancellationToken),
            async service =>
            {
                var actions = new[] { "test_successful", "talk_to_user" };
                var chat = new ChatSessionData
                {
                    Culture = "en-US",
                    User = new ChatSessionDataUser { Name = "User" },
                    Chat = new Chat { Id = Guid.Empty, CharacterId = Guid.Empty },
                    Character = new ChatSessionDataCharacter
                    {
                        Name = "Assistant",
                        SystemPrompt = "You are a test assistant",
                        Description = "",
                        Personality = "",
                        Scenario = "This is a test",
                        FirstMessage = "Beginning test.",
                    },
                    Actions = actions
                };
                chat.AddMessage(chat.Character, "I like apples. Do you like apples?");
                chat.AddMessage(chat.User, "No, I don't like them at all.");
                var result = await service.SummarizeAsync(chat, chat.Messages, cancellationToken);
                return ("Summary: " + result, result.Contains("apple", StringComparison.InvariantCultureIgnoreCase));
            }
        );
    }
    
    private async Task<ServiceDiagnosticsResult> TrySpeechToText(ConfiguredService s, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            s,
            runTests,
            async () => await _speechToTextFactory.CreateSpecificAsync(new ServiceLink(s), "en-US", !runTests, cancellationToken),
            _ => Task.FromResult(("Cannot be tested automatically", false))
        );
    }
    
    private static async Task<ServiceDiagnosticsResult> TestServiceAsync<TService>(ConfiguredService s, bool runTests, Func<Task<TService>> createService, Func<TService, Task<(string, bool)>> testService) where TService : IService
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
                    ServiceName = s.ServiceName,
                    ServiceId = s.Id,
                    Label = s.ToString(),
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
                    ServiceName = s.ServiceName,
                    ServiceId = s.Id,
                    Label = s.ToString(),
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
                    ServiceName = s.ServiceName,
                    ServiceId = s.Id,
                    Label = s.ToString(),
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
                    ServiceName = s.ServiceName,
                    ServiceId = s.Id,
                    Label = s.ToString(),
                    Status = "Configured",
                    Features = features
                };
            }

            try
            {
                var (status, success) = await testService(service);
                
                return new ServiceDiagnosticsResult
                {
                    IsReady = true,
                    IsHealthy = success,
                    IsTested = true,
                    ServiceName = s.ServiceName,
                    ServiceId = s.Id,
                    Label = s.ToString(),
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
                    ServiceName = s.ServiceName,
                    ServiceId = s.Id,
                    Label = s.ToString(),
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
                    ServiceName = s.ServiceName,
                    ServiceId = s.Id,
                    Label = s.ToString(),
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
