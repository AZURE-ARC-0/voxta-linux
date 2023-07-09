using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatSessionFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ChatRepositories _repositories;
    private readonly ExclusiveLocalInputManager _localInputManager;
    private readonly SpeechGeneratorFactory _speechGeneratorFactory;
    private readonly IServiceFactory<ITextGenService> _textGenFactory;
    private readonly IServiceFactory<ITextToSpeechService> _textToSpeechFactory;
    private readonly IServiceFactory<IAnimationSelectionService> _animationSelectionFactory;

    public ChatSessionFactory(
        ILoggerFactory loggerFactory,
        ChatRepositories repositories,
        ExclusiveLocalInputManager localInputManager,
        SpeechGeneratorFactory speechGeneratorFactory,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<IAnimationSelectionService> animationSelectionFactory)
    {
        _loggerFactory = loggerFactory;
        _repositories = repositories;
        _localInputManager = localInputManager;
        _speechGeneratorFactory = speechGeneratorFactory;
        _textGenFactory = textGenFactory;
        _textToSpeechFactory = textToSpeechFactory;
        _animationSelectionFactory = animationSelectionFactory;
    }

    public async Task<IChatSession> CreateAsync(IUserConnectionTunnel tunnel, ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        if (startChatMessage.AudioPath != null)
        {
            Directory.CreateDirectory(startChatMessage.AudioPath);
        }
        
        var profile = await _repositories.Profile.GetProfileAsync(cancellationToken) ?? new ProfileSettings { Name = "User", Description = "" };
        var textProcessor = new ChatTextProcessor(profile, startChatMessage.BotName);

        var textGen = await _textGenFactory.CreateAsync(startChatMessage.TextGenService, cancellationToken);
        var animationSelection = string.IsNullOrEmpty(profile.AnimationSelectionService)
            ? null
            : await _animationSelectionFactory.CreateAsync(profile.AnimationSelectionService, cancellationToken);
        
        string[]? thinkingSpeech = null;
        if (startChatMessage is { TtsService: not null, TtsVoice: not null })
        {
            var textToSpeechGen = await _textToSpeechFactory.CreateAsync(startChatMessage.TtsService, cancellationToken);
            thinkingSpeech = textToSpeechGen.GetThinkingSpeech();
        }

        var speechGenerator = await _speechGeneratorFactory.CreateAsync(startChatMessage.TtsService, startChatMessage.TtsVoice, startChatMessage.AudioPath, cancellationToken);
        
        // TODO: Use a real chat data store, reload using auth
        var chatData = new ChatSessionData
        {
            ChatId = startChatMessage.ChatId ?? Crypto.CreateCryptographicallySecureGuid(),
            UserName = profile.Name,
            BotName = startChatMessage.BotName,
            Preamble = new TextData
            {
                Text = textProcessor.ProcessText(startChatMessage.Preamble)
            },
            Postamble = new TextData
            {
                Text = textProcessor.ProcessText(startChatMessage.Postamble)
            },
            Greeting = !string.IsNullOrEmpty(startChatMessage.Greeting) ? new TextData
            {
                Text = textProcessor.ProcessText(startChatMessage.Greeting)
            } : null,
            ThinkingSpeech = thinkingSpeech,
            AudioPath = startChatMessage.AudioPath,
            TtsVoice = startChatMessage.TtsVoice
        };
        chatData.Preamble.Tokens = textGen.GetTokenCount(chatData.Preamble.Text);
        chatData.Postamble.Tokens = textGen.GetTokenCount(chatData.Postamble.Text);
        if(chatData.Greeting != null) chatData.Greeting.Tokens = textGen.GetTokenCount(chatData.Greeting.Text);
        var sampleMessages = startChatMessage.SampleMessages?.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        foreach (var message in sampleMessages)
        {
            var parts = message.Split(":");
            if (parts.Length == 1) continue;
            var m = new ChatMessageData
            {
                User = parts[0] switch
                {
                    "{{User}}" => profile.Name,
                    "{{Bot}}" => startChatMessage.BotName,
                    _ => startChatMessage.BotName
                },
                Text = textProcessor.ProcessText(parts[1].Trim())
            };
            m.Tokens = textGen.GetTokenCount(m.Text);
            chatData.SampleMessages.Add(m);
        }
        
        var useSpeechRecognition = startChatMessage.UseServerSpeechRecognition && profile.EnableSpeechRecognition;

        return new ChatSession(
            tunnel,
            _loggerFactory,
            textGen,
            chatData,
            textProcessor,
            profile,
            useSpeechRecognition ? _localInputManager.Acquire() : null,
            new ChatSessionState(),
            speechGenerator,
            animationSelection
        );
    }
}