using System.Runtime.ExceptionServices;
using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Microsoft.Extensions.Logging;
using Voxta.Abstractions.System;

namespace Voxta.Core;

public class ChatSessionFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IProfileRepository _profileRepository;
    private readonly SpeechGeneratorFactory _speechGeneratorFactory;
    private readonly IServiceFactory<ITextGenService> _textGenFactory;
    private readonly IServiceFactory<ITextToSpeechService> _textToSpeechFactory;
    private readonly IServiceFactory<IActionInferenceService> _animationSelectionFactory;
    private readonly IServiceFactory<ISpeechToTextService> _speechToTextServiceFactory;
    private readonly ITimeProvider _timeProvider;

    public ChatSessionFactory(
        ILoggerFactory loggerFactory,
        IPerformanceMetrics performanceMetrics,
        IProfileRepository profileRepository,
        SpeechGeneratorFactory speechGeneratorFactory,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<IActionInferenceService> animationSelectionFactory,
        IServiceFactory<ISpeechToTextService> speechToTextServiceFactory,
        ITimeProvider timeProvider
        )
    {
        _loggerFactory = loggerFactory;
        _performanceMetrics = performanceMetrics;
        _profileRepository = profileRepository;
        _speechGeneratorFactory = speechGeneratorFactory;
        _textGenFactory = textGenFactory;
        _textToSpeechFactory = textToSpeechFactory;
        _animationSelectionFactory = animationSelectionFactory;
        _speechToTextServiceFactory = speechToTextServiceFactory;
        _timeProvider = timeProvider;
        _profileRepository = profileRepository;
    }

    public async Task<IChatSession> CreateAsync(IUserConnectionTunnel tunnel, ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        ITextGenService? textGen = null;
        ISpeechToTextService? speechToText = null;
        IActionInferenceService? actionInference = null;
        ISpeechGenerator? speechGenerator = null;

        try
        {
            if (startChatMessage.AudioPath != null)
            {
                Directory.CreateDirectory(startChatMessage.AudioPath);
            }
            
            var profile = await _profileRepository.GetRequiredProfileAsync(cancellationToken);
            var useSpeechRecognition = startChatMessage.UseServerSpeechRecognition && profile.SpeechToText.Services.Any();

            var prerequisites = startChatMessage.Prerequisites != null ? startChatMessage.Prerequisites.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
            textGen = await _textGenFactory.CreateAsync(profile.TextGen, startChatMessage.TextGenService, prerequisites, startChatMessage.Culture, cancellationToken);
            speechToText = useSpeechRecognition ? await _speechToTextServiceFactory.CreateAsync(profile.SpeechToText, startChatMessage.SttService ?? "", prerequisites, startChatMessage.Culture, cancellationToken) : null;
            actionInference = profile.ActionInference.Services.Any()
                ? await _animationSelectionFactory.CreateAsync(profile.ActionInference, startChatMessage.ActionInferenceService ?? "", prerequisites, startChatMessage.Culture, cancellationToken)
                : null;

            var textProcessor = new ChatTextProcessor(profile, startChatMessage.Name);
            
            var textToSpeechGen = await _textToSpeechFactory.CreateAsync(profile.TextToSpeech, startChatMessage.TtsService ?? "", prerequisites, startChatMessage.Culture, cancellationToken);
            var thinkingSpeech = textToSpeechGen.GetThinkingSpeech();

            speechGenerator = _speechGeneratorFactory.Create(textToSpeechGen, startChatMessage.TtsVoice, startChatMessage.Culture, startChatMessage.AudioPath, startChatMessage.AcceptedAudioContentTypes, cancellationToken);

            // TODO: Use a real chat data store, reload using auth
            var chatData = new ChatSessionData
            {
                ChatId = startChatMessage.ChatId ?? Crypto.CreateCryptographicallySecureGuid(),
                UserName = profile.Name,
                Character = new CharacterCard
                {
                    Name = startChatMessage.Name,
                    Description = textProcessor.ProcessText(startChatMessage.Description),
                    Personality = textProcessor.ProcessText(startChatMessage.Personality),
                    Scenario = textProcessor.ProcessText(startChatMessage.Scenario),
                    FirstMessage = textProcessor.ProcessText(startChatMessage.FirstMessage),
                    MessageExamples = textProcessor.ProcessText(startChatMessage.MessageExamples),
                    SystemPrompt = textProcessor.ProcessText(startChatMessage.SystemPrompt),
                    PostHistoryInstructions = textProcessor.ProcessText(startChatMessage.PostHistoryInstructions),
                },
                ThinkingSpeech = thinkingSpeech,
                AudioPath = startChatMessage.AudioPath,
                TtsVoice = startChatMessage.TtsVoice
            };
            // TODO: Optimize by pre-calculating tokens count

            var state = new ChatSessionState(_timeProvider);

            return new ChatSession(
                tunnel,
                _loggerFactory,
                _performanceMetrics,
                textGen,
                chatData,
                textProcessor,
                profile,
                state,
                speechGenerator,
                actionInference,
                speechToText
            );
        }
        catch (Exception exc)
        {
            textGen?.Dispose();
            speechToText?.Dispose();
            actionInference?.Dispose();
            speechGenerator?.Dispose();
            ExceptionDispatchInfo.Capture(exc).Throw();
            throw;
        }
    }
}