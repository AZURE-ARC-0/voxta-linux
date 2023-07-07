using ChatMate.Abstractions.Model;

namespace ChatMate.Core;

public partial class ChatSession
{
    private async Task SendReplyWithSpeechAsync(ChatMessageData reply, CancellationToken cancellationToken)
    {
        var speechTask = Task.Run(() => _speechGenerator.CreateSpeechAsync(reply.Text, $"msg_{_chatSessionData.ChatId.ToString()}_{reply.Id}", cancellationToken), cancellationToken);

        await _tunnel.SendAsync(new ServerReplyMessage
        {
            Text = reply.Text,
        }, cancellationToken);

        var speechUrl = await speechTask;
        if (speechUrl != null)
        {
            if (_pauseSpeechRecognitionDuringPlayback) _inputHandle?.RequestPauseSpeechRecognition();
            await _tunnel.SendAsync(new ServerSpeechMessage
            {
                Url = speechUrl,
            }, cancellationToken);
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
}