using ChatMate.Abstractions.Model;

namespace ChatMate.Core;

public partial class ChatSession
{
    private async Task SendReply(ChatMessageData reply, CancellationToken cancellationToken)
    {
        var speechTask = _speechGenerator.CreateSpeechAsync(reply.Text, $"msg_{_chatSessionData.ChatId.ToString()}_{reply.Id}", cancellationToken);

        await _tunnel.SendAsync(new ServerReplyMessage
        {
            Text = reply.Text,
        }, cancellationToken);

        var speechUrl = await speechTask;
        if (speechUrl != null)
        {
            await _tunnel.SendAsync(new ServerSpeechMessage
            {
                Url = speechUrl,
            }, cancellationToken);
            _chatSessionState.SpeechStart();
        }

#warning Re-enable this but not as a bot option
        /*
        if (_servicesLocator.AnimSelectFactory.TryCreate(bot.Services.AnimSelect.Service, out var animSelect))
        {
            var animation = await animSelect.SelectAnimationAsync(chatData);
            _logger.LogInformation("Selected animation: {Animation}", animation);
            await _tunnel.SendAsync(new ServerAnimationMessage { Value = animation }, cancellationToken);
        }
        */
    }

    private ChatMessageData CreateMessageFromGen(TextData gen)
    {
        var reply = new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _chatSessionData.BotName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
        // TODO: Save into some storage
        _chatSessionData.Messages.Add(reply);
        return reply;
    }
}