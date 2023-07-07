using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class HandleReadyMessageProcessing : ReplyMessageProcessingBase, IMessageProcessing
{
    private readonly ILogger<HandleClientMessageProcessing> _logger;
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ChatSessionData _chatSessionData;

    public HandleReadyMessageProcessing(IUserConnectionTunnel tunnel, ILoggerFactory loggerFactory,
        ChatSessionData chatSessionData, ChatServices services,
        ChatSessionState chatSessionState, ClientStartChatMessage startChatMessage, PendingSpeechManager pendingSpeech, ITemporaryFileCleanup temporaryFileCleanup)
        : base(tunnel, chatSessionData, chatSessionState, startChatMessage, services, pendingSpeech, temporaryFileCleanup)
    {
        _logger = loggerFactory.CreateLogger<HandleClientMessageProcessing>();
        _tunnel = tunnel;
        _chatSessionData = chatSessionData;
    }

    public async ValueTask HandleAsync(CancellationToken cancellationToken)
    {
#warning Bring back thinking speech
        /*
        var thinkingSpeechUrls = new string[_bot.ThinkingSpeech?.Length ?? 0];
        if (_bot.ThinkingSpeech != null)
        {
            byte i = 0;
            foreach (var thinkingSpeech in _bot.ThinkingSpeech)
            {
                await CreateSpeech(thinkingSpeech, Crypto.CreateCryptographicallySecureGuid().ToString(), out var speechUrl);
                thinkingSpeechUrls[i] = speechUrl;
                i++;
            }
        }
        */

        await _tunnel.SendAsync(new ServerReadyMessage
        {
            ChatId = _chatSessionData.ChatId,
            // ThinkingSpeechUrls = thinkingSpeechUrls,
                
        }, cancellationToken);
        
        _logger.LogInformation("Chat ready!");

        if (_chatSessionData.Greeting != null)
        {
            var reply = CreateMessageFromGen(_chatSessionData.Greeting);
            await SendReply(reply, cancellationToken);
        }
    }
}