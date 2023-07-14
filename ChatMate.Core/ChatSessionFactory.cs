using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

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

    public ChatSessionFactory(
        ILoggerFactory loggerFactory,
        IPerformanceMetrics performanceMetrics,
        IProfileRepository profileRepository,
        SpeechGeneratorFactory speechGeneratorFactory,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<IActionInferenceService> animationSelectionFactory,
        IServiceFactory<ISpeechToTextService> speechToTextServiceFactory
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
        _profileRepository = profileRepository;
    }

    public async Task<IChatSession> CreateAsync(IUserConnectionTunnel tunnel, ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        if (startChatMessage.AudioPath != null)
        {
            Directory.CreateDirectory(startChatMessage.AudioPath);
        }

        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        if (profile == null) throw new InvalidOperationException("Cannot start chat, no profile is set.");
        var textProcessor = new ChatTextProcessor(profile, startChatMessage.Name);

        var textGen = await _textGenFactory.CreateAsync(startChatMessage.TextGenService, cancellationToken);
        var actionInference = string.IsNullOrEmpty(profile.Services.ActionInference.Service)
            ? null
            : await _animationSelectionFactory.CreateAsync(profile.Services.ActionInference.Service, cancellationToken);
        
        string[]? thinkingSpeech = null;
        if (startChatMessage is { TtsService: not null, TtsVoice: not null })
        {
            var textToSpeechGen = await _textToSpeechFactory.CreateAsync(startChatMessage.TtsService, cancellationToken);
            thinkingSpeech = textToSpeechGen.GetThinkingSpeech();
        }

        var speechGenerator = await _speechGeneratorFactory.CreateAsync(startChatMessage.TtsService, startChatMessage.TtsVoice, startChatMessage.AudioPath, startChatMessage.AcceptedAudioContentTypes, cancellationToken);
        
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
        
        var useSpeechRecognition = startChatMessage.UseServerSpeechRecognition && profile.EnableSpeechRecognition;
        
        var speechToText = useSpeechRecognition ? await _speechToTextServiceFactory.CreateAsync(profile.Services.SpeechToText.Service, cancellationToken) : null;
        
        return new ChatSession(
            tunnel,
            _loggerFactory,
            _performanceMetrics,
            textGen,
            chatData,
            textProcessor,
            profile,
            new ChatSessionState(),
            speechGenerator,
            actionInference,
            speechToText
        );
    }
}