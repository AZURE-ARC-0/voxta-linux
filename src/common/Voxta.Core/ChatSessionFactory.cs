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
    private readonly IChatRepository _chatRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
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
        IChatRepository chatRepository,
        IChatMessageRepository chatMessageRepository,
        SpeechGeneratorFactory speechGeneratorFactory,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<IActionInferenceService> animationSelectionFactory,
        IServiceFactory<ISpeechToTextService> speechToTextServiceFactory,
        ITimeProvider timeProvider)
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
        _timeProvider = timeProvider;
        _profileRepository = profileRepository;
    }

    public async Task<IChatSession> CreateAsync(IUserConnectionTunnel tunnel, ClientResumeChatMessage resumeChatMessage, CancellationToken cancellationToken)
    {
        var chat = await _chatRepository.GetChatAsync(resumeChatMessage.ChatId.ToString(), cancellationToken);
        if (chat == null) throw new NullReferenceException($"Could not find chat {resumeChatMessage.ChatId}");
        return await CreateAsync(
            tunnel,
            resumeChatMessage.ChatId,
            chat.Character,
            resumeChatMessage,
            cancellationToken
        );
    }

    public async Task<IChatSession> CreateAsync(IUserConnectionTunnel tunnel, ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        var character = new CharacterCardExtended
        {
            Name = startChatMessage.Name,
            Description = startChatMessage.Description,
            Personality = startChatMessage.Personality,
            Scenario = startChatMessage.Scenario,
            FirstMessage = startChatMessage.FirstMessage ?? "",
            MessageExamples = startChatMessage.MessageExamples,
            SystemPrompt = startChatMessage.SystemPrompt,
            PostHistoryInstructions = startChatMessage.PostHistoryInstructions,
            Prerequisites = startChatMessage.Prerequisites?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            Culture = startChatMessage.Culture,
            Services = new CharacterServicesMap
            {
                SpeechGen = new VoiceServiceMap
                {
                    Service = startChatMessage.TtsService,
                    Voice = startChatMessage.TtsVoice,
                },
                TextGen = new ServiceMap
                {
                    Service = startChatMessage.TextGenService,
                },
                ActionInference = new ServiceMap
                {
                    Service = startChatMessage.ActionInferenceService
                }
            }
        };
        #warning TODO
        /*
        await _chatRepository.SaveChatAsync(new Chat
        {
            Id = startChatMessage.ChatId,
            Character = character
        });
        */
        return await CreateAsync(
            tunnel,
            startChatMessage.ChatId,
            character,
            startChatMessage,
            cancellationToken
        );
    }

    private async Task<IChatSession> CreateAsync(IUserConnectionTunnel tunnel, Guid? chatId, CharacterCardExtended character, ClientDoChatMessageBase startChatMessage, CancellationToken cancellationToken)
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

            var prerequisites = character.Prerequisites ?? Array.Empty<string>();
            var culture = character.Culture;
            textGen = await _textGenFactory.CreateAsync(profile.TextGen, character.Services.TextGen?.Service ?? "", prerequisites, culture, cancellationToken);
            speechToText = useSpeechRecognition ? await _speechToTextServiceFactory.CreateAsync(profile.SpeechToText, "", prerequisites, culture, cancellationToken) : null;
            actionInference = profile.ActionInference.Services.Any()
                ? await _animationSelectionFactory.CreateAsync(profile.ActionInference, character.Services.ActionInference?.Service ?? "", prerequisites, culture, cancellationToken)
                : null;

            var textProcessor = new ChatTextProcessor(profile, character.Name);
            
            var textToSpeechGen = await _textToSpeechFactory.CreateAsync(profile.TextToSpeech, character.Services.SpeechGen?.Service ?? "", prerequisites, culture, cancellationToken);
            var thinkingSpeech = textToSpeechGen.GetThinkingSpeech();

            speechGenerator = _speechGeneratorFactory.Create(textToSpeechGen, character.Services.SpeechGen?.Voice, culture, startChatMessage.AudioPath, startChatMessage.AcceptedAudioContentTypes, cancellationToken);

            var messages = chatId.HasValue ? await _chatMessageRepository.GetChatMessagesAsync(chatId.Value.ToString(), cancellationToken) : null;
            
            var chatData = new ChatSessionData
            {
                ChatId = chatId ?? Crypto.CreateCryptographicallySecureGuid(),
                UserName = profile.Name,
                Character = new CharacterCardExtended
                {
                    
                    Name = character.Name,
                    Description = textProcessor.ProcessText(character.Description),
                    Personality = textProcessor.ProcessText(character.Personality),
                    Scenario = textProcessor.ProcessText(character.Scenario),
                    FirstMessage = textProcessor.ProcessText(character.FirstMessage),
                    MessageExamples = textProcessor.ProcessText(character.MessageExamples),
                    SystemPrompt = textProcessor.ProcessText(character.SystemPrompt),
                    PostHistoryInstructions = textProcessor.ProcessText(character.PostHistoryInstructions),
                    Prerequisites = character.Prerequisites,
                    Culture = character.Culture,
                    Services = character.Services
                },
                ThinkingSpeech = thinkingSpeech,
                AudioPath = startChatMessage.AudioPath,
            };
            if (messages != null)
            {
                chatData.Messages.AddRange(messages);
            }
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