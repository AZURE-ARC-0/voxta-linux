using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatSessionFactory
{
    private readonly ChatServicesFactories _servicesFactories;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ChatRepositories _repositories;
    private readonly ExclusiveLocalInputManager _localInputManager;
    private readonly ITemporaryFileCleanup _temporaryFileCleanup;
    private readonly PendingSpeechManager _pendingSpeech;

    public ChatSessionFactory(ChatServicesFactories servicesFactories, ILoggerFactory loggerFactory, ChatRepositories repositories, ExclusiveLocalInputManager localInputManager, ITemporaryFileCleanup temporaryFileCleanup, PendingSpeechManager pendingSpeech)
    {
        _servicesFactories = servicesFactories;
        _loggerFactory = loggerFactory;
        _repositories = repositories;
        _localInputManager = localInputManager;
        _temporaryFileCleanup = temporaryFileCleanup;
        _pendingSpeech = pendingSpeech;
    }

    public async Task<ChatSession> CreateAsync(IUserConnectionTunnel tunnel, ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        if (startChatMessage.AudioPath != null)
        {
            Directory.CreateDirectory(startChatMessage.AudioPath);
        }
        
        var profile = await _repositories.Profile.GetProfileAsync(cancellationToken) ?? new ProfileSettings { Name = "User", Description = "" };
        var textProcessor = new ChatTextProcessor(profile, startChatMessage.BotName);

        var services = _servicesFactories.Create(startChatMessage.TextGenService, startChatMessage.TtsService);
        
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
            } : null
        };
        chatData.Preamble.Tokens = services.TextGen.GetTokenCount(chatData.Preamble.Text);
        chatData.Postamble.Tokens = services.TextGen.GetTokenCount(chatData.Postamble.Text);
        if(chatData.Greeting != null) chatData.Greeting.Tokens = services.TextGen.GetTokenCount(chatData.Greeting.Text);
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
            m.Tokens = services.TextGen.GetTokenCount(m.Text);
            chatData.SampleMessages.Add(m);
        }
        
        var useSpeechRecognition = startChatMessage.UseServerSpeechRecognition && profile.EnableSpeechRecognition;

        return new ChatSession(
            tunnel,
            _loggerFactory,
            services,
            chatData,
            startChatMessage,
            textProcessor,
            profile,
            useSpeechRecognition ? _localInputManager.Acquire() : null,
            _temporaryFileCleanup,
            _pendingSpeech,
            new ChatSessionState()
        );
    }
}