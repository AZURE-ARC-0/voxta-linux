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
    public required ServiceDiagnosticsResult Profile { get; init; }
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

    public DiagnosticsUtil(
        IProfileRepository profileRepository,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<ISpeechToTextService> speechToTextFactory,
        IServiceFactory<IActionInferenceService> actionInferenceFactory,
        IServiceFactory<ISummarizationService> summarizationFactory)
    {
        _profileRepository = profileRepository;
        _textGenFactory = textGenFactory;
        _textToSpeechFactory = textToSpeechFactory;
        _speechToTextFactory = speechToTextFactory;
        _actionInferenceFactory = actionInferenceFactory;
        _summarizationFactory = summarizationFactory;
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
                SummarizationServices = Array.Empty<ServiceDiagnosticsResult>(),
            };
        }

        var textGen = _textGenFactory.ServiceNames.Select(serviceName =>
            Task.Run(async () => await TryTextGenAsync(serviceName, runTests, cancellationToken), cancellationToken)
        ).ToArray();
        
        var tts = _textToSpeechFactory.ServiceNames.Select(serviceName =>
            Task.Run(async () => await TryTextToSpeechAsync(serviceName, runTests, cancellationToken), cancellationToken)
        ).ToArray();
        
        var actionInference = _actionInferenceFactory.ServiceNames.Select(serviceName =>
            Task.Run(async () => await TryActionInferenceAsync(serviceName, runTests, cancellationToken), cancellationToken)
        ).ToArray();
        
        var summarization = _summarizationFactory.ServiceNames.Select(serviceName =>
            Task.Run(async () => await TrySummarizationAsync(serviceName, runTests, cancellationToken), cancellationToken)
        ).ToArray();

        var sttNames = _speechToTextFactory.ServiceNames.ToArray();
        var stt = new ServiceDiagnosticsResult[sttNames.Length];
        var sttTask = Task.Run(async () =>
        {
            for (var i = 0; i < sttNames.Length; i++)
            {
                stt[i] = await TrySpeechToText(sttNames[i], runTests, cancellationToken);
            }
        }, cancellationToken);

        await Task.WhenAll(textGen.Concat(tts).Concat(actionInference).Concat(summarization).Concat(new[] { sttTask }));

        return new DiagnosticsResult
        {
            Profile = profileResult,
            TextGenServices = textGen.Select(t => t.Result).OrderBy(x => profile.TextGen.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
            TextToSpeechServices = tts.Select(t => t.Result).OrderBy(x => profile.TextToSpeech.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
            ActionInferenceServices = actionInference.Select(t => t.Result).OrderBy(x => profile.ActionInference.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
            SummarizationServices = summarization.Select(t => t.Result).OrderBy(x => profile.Summarization.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
            SpeechToTextServices = stt.OrderBy(x => profile.SpeechToText.Order(x.ServiceName)).ThenBy(x => x.ServiceName).ToArray(),
        };
    }

    private async Task<ServiceDiagnosticsResult> TryTextGenAsync(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            serviceName,
            runTests,
            async () => await _textGenFactory.CreateSpecificAsync(serviceName, "en-US", !runTests, cancellationToken),
            async service =>
            {
                var chat = new ChatSessionData
                {
                    Culture = "en-US",
                    User = new ChatSessionDataUser { Name = "User" },
                    Chat = null!,
                    Character = new ChatSessionDataCharacter
                    {
                        Name = "Assistant",
                        SystemPrompt = "You are a test assistant",
                        Description = "",
                        Personality = "",
                        Scenario = "This is a test",
                        FirstMessage = "Beginning test.",
                    }
                };
                chat.AddMessage(chat.Character.Name, chat.Character.FirstMessage);
                chat.AddMessage(chat.User.Name, "Are you working correctly?");
                var result = await service.GenerateReplyAsync(chat, cancellationToken);
                return "Response: " + result;
            }
        );
    }
    
    private async Task<ServiceDiagnosticsResult> TryTextToSpeechAsync(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            serviceName,
            runTests,
            async () => await _textToSpeechFactory.CreateSpecificAsync(serviceName, "en-US", !runTests, cancellationToken),
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
    
    private async Task<ServiceDiagnosticsResult> TryActionInferenceAsync(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            serviceName,
            runTests,
            async () => await _actionInferenceFactory.CreateSpecificAsync(serviceName, "en-US", !runTests, cancellationToken),
            async service =>
            {
                var actions = new[] { "test_successful", "test_failed" };
                var chat = new ChatSessionData
                {
                    Culture = "en-US",
                    User = new ChatSessionDataUser { Name = "User" },
                    Chat = null!,
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
                chat.AddMessage(chat.Character.Name, "Yep, looks like this is working!");
                var result = await service.SelectActionAsync(chat, cancellationToken);
                return "Action: " + result;
            }
        );
    }
    
    private async Task<ServiceDiagnosticsResult> TrySummarizationAsync(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            serviceName,
            runTests,
            async () => await _summarizationFactory.CreateSpecificAsync(serviceName, "en-US", !runTests, cancellationToken),
            async service =>
            {
                var actions = new[] { "test_successful", "talk_to_user" };
                var chat = new ChatSessionData
                {
                    Culture = "en-US",
                    User = new ChatSessionDataUser { Name = "User" },
                    Chat = null!,
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
                chat.AddMessage(chat.Character.Name, "I like apples. Do you like apples?");
                chat.AddMessage(chat.User.Name, "No, I don't like them at all.");
                var result = await service.SummarizeAsync(chat, cancellationToken);
                return "Summary: " + result;
            }
        );
    }
    
    private async Task<ServiceDiagnosticsResult> TrySpeechToText(string serviceName, bool runTests, CancellationToken cancellationToken)
    {
        return await TestServiceAsync(
            serviceName,
            runTests,
            async () => await _speechToTextFactory.CreateSpecificAsync(serviceName, "en-US", !runTests, cancellationToken),
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
