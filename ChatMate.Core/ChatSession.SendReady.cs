using ChatMate.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public partial class ChatSession
{
    public void SendReady()
    {
        Enqueue(HandleAsync);
    }

    private async ValueTask HandleAsync(CancellationToken cancellationToken)
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