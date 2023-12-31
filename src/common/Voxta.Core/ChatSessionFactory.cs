﻿using System.Globalization;
using System.Runtime.ExceptionServices;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Voxta.Abstractions;
using Voxta.Abstractions.System;

namespace Voxta.Core;

public class ChatSessionFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IProfileRepository _profileRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly SpeechGeneratorFactory _speechGeneratorFactory;
    private readonly IServiceFactory<ITextGenService> _textGenFactory;
    private readonly IServiceFactory<ITextToSpeechService> _textToSpeechFactory;
    private readonly IServiceFactory<IActionInferenceService> _animationSelectionFactory;
    private readonly IServiceFactory<ISpeechToTextService> _speechToTextServiceFactory;
    private readonly IServiceFactory<ISummarizationService> _summarizationServiceFactory;
    private readonly ITimeProvider _timeProvider;
    private readonly IMemoryProvider _memoryProvider;

    public ChatSessionFactory(
        ILoggerFactory loggerFactory,
        IPerformanceMetrics performanceMetrics,
        IProfileRepository profileRepository,
        IChatRepository chatRepository,
        IChatMessageRepository chatMessageRepository,
        SpeechGeneratorFactory speechGeneratorFactory,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<IActionInferenceService> animationSelectionFactory,
        IServiceFactory<ISpeechToTextService> speechToTextServiceFactory,
        IServiceFactory<ISummarizationService> summarizationServiceFactory,
        ITimeProvider timeProvider,
        IMemoryProvider memoryProvider)
    {
        _loggerFactory = loggerFactory;
        _performanceMetrics = performanceMetrics;
        _profileRepository = profileRepository;
        _chatRepository = chatRepository;
        _chatMessageRepository = chatMessageRepository;
        _speechGeneratorFactory = speechGeneratorFactory;
        _textGenFactory = textGenFactory;
        _textToSpeechFactory = textToSpeechFactory;
        _animationSelectionFactory = animationSelectionFactory;
        _speechToTextServiceFactory = speechToTextServiceFactory;
        _summarizationServiceFactory = summarizationServiceFactory;
        _timeProvider = timeProvider;
        _memoryProvider = memoryProvider;
    }

    public async Task<IChatSession> CreateAsync(IUserConnectionTunnel tunnel, Chat chat, CharacterCardExtended character, ClientDoChatMessageBase startChatMessage, CancellationToken cancellationToken)
    {
        ITextGenService? textGen = null;
        ISpeechToTextService? speechToText = null;
        IActionInferenceService? actionInference = null;
        ISpeechGenerator? speechGenerator = null;
        ISummarizationService? summarizationService = null;

        try
        {
            if (startChatMessage.AudioPath != null)
            {
                Directory.CreateDirectory(startChatMessage.AudioPath);
            }
            
            var profile = await _profileRepository.GetRequiredProfileAsync(cancellationToken);
            var useSpeechRecognition = startChatMessage.UseServerSpeechRecognition && profile.SpeechToText.Services.Any();

            var prerequisites = profile.IgnorePrerequisites
                ? (IPrerequisitesValidator)new IgnorePrerequisitesValidator()
                : new PrerequisitesValidator(character);
            var culture = character.Culture;
            textGen = await _textGenFactory.CreateBestMatchRequiredAsync(profile.TextGen, character.Services.TextGen.Service, prerequisites, culture, cancellationToken);
            speechToText = useSpeechRecognition ? await _speechToTextServiceFactory.CreateBestMatchAsync(profile.SpeechToText, null, prerequisites, culture, cancellationToken) : null;
            actionInference = startChatMessage.UseActionInference && profile.ActionInference.Services.Any()
                ? await _animationSelectionFactory.CreateBestMatchAsync(profile.ActionInference, character.Services.ActionInference.Service, prerequisites, culture, cancellationToken)
                : null;
            summarizationService = await _summarizationServiceFactory.CreateBestMatchAsync(profile.Summarization, null, prerequisites, character.Culture, cancellationToken);

            var cultureInfo = CultureInfo.GetCultureInfoByIetfLanguageTag(culture);
            var textProcessor = new ChatTextProcessor(profile, character.Name, cultureInfo, textGen);
            
            var textToSpeechGen = await _textToSpeechFactory.CreateBestMatchAsync(profile.TextToSpeech, character.Services.SpeechGen.Service, prerequisites, culture, cancellationToken);
            var thinkingSpeech = textToSpeechGen?.GetThinkingSpeech() ?? Array.Empty<string>();

            speechGenerator = _speechGeneratorFactory.Create(textToSpeechGen, character.Services.SpeechGen.Voice, culture, startChatMessage.AudioPath, startChatMessage.AcceptedAudioContentTypes, cancellationToken);

            var messages = await _chatMessageRepository.GetChatMessagesAsync(chat.Id, false, cancellationToken);

            var chatData = new ChatSessionData
            {
                Chat = chat,
                Culture = character.Culture,
                User = new ChatSessionDataUser
                {
                    Name = textProcessor.ProcessText(profile.Name),
                    Description = textProcessor.ProcessText(profile.Description)
                },
                Character = new ChatSessionDataCharacter
                {
                    Name = textProcessor.ProcessText(character.Name),
                    Description = textProcessor.ProcessText(character.Description),
                    Personality = textProcessor.ProcessText(character.Personality),
                    Scenario = textProcessor.ProcessText(character.Scenario),
                    FirstMessage = textProcessor.ProcessText(character.FirstMessage),
                    MessageExamples = textProcessor.ProcessText(character.MessageExamples),
                    SystemPrompt = textProcessor.ProcessText(character.SystemPrompt),
                    PostHistoryInstructions = textProcessor.ProcessText(character.PostHistoryInstructions),
                },
                ThinkingSpeech = thinkingSpeech,
                AudioPath = startChatMessage.AudioPath,
            };
            using (var token = chatData.GetWriteToken())
            {
                token.Messages.AddRange(messages);
            }
            // TODO: Optimize by pre-calculating tokens count

            await _memoryProvider.Initialize(chat.CharacterId, textProcessor, cancellationToken);
            
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
                speechToText,
                summarizationService,
                _chatRepository,
                _chatMessageRepository,
                _memoryProvider
            );
        }
        catch (Exception exc)
        {
            if(textGen != null) await textGen.DisposeAsync();
            if(speechToText != null) await speechToText.DisposeAsync();
            if(actionInference != null) await actionInference.DisposeAsync();
            if(speechGenerator != null) await speechGenerator.DisposeAsync();
            if(summarizationService != null) await summarizationService.DisposeAsync();
            ExceptionDispatchInfo.Capture(exc).Throw();
            throw;
        }
    }
}
