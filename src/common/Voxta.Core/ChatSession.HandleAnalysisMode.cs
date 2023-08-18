using System.Text;
using Voxta.Abstractions.Model;

namespace Voxta.Core;

public partial class ChatSession
{
    private async Task HandleAnalysisModeAsync(ClientSendMessage message, CancellationToken cancellationToken)
    {
        var text = message.Text;
        if (text.StartsWith("help", StringComparison.InvariantCultureIgnoreCase))
            await HelpAsync(cancellationToken);
        else if (text.StartsWith("list services", StringComparison.InvariantCultureIgnoreCase))
            await ListServicesAsync(cancellationToken);
        else if (text.StartsWith("list commands", StringComparison.InvariantCultureIgnoreCase))
            await ListCommandsAsync(cancellationToken);
        else if (text.StartsWith("reset chat", StringComparison.InvariantCultureIgnoreCase))
            await ResetChatAsync(cancellationToken);
        else if (text.StartsWith("repeat", StringComparison.InvariantCultureIgnoreCase) && text.Length > 7)
            await RepeatAsync(text, cancellationToken);
        else if (text.StartsWith("regenerate", StringComparison.InvariantCultureIgnoreCase) && text.Length > 7)
            await RegenerateAsync(cancellationToken);
        else if (text.StartsWith("rollback", StringComparison.InvariantCultureIgnoreCase) && text.Length > 7)
            await RollbackAsync(cancellationToken);
        else
            await SendReusableReplyWithSpeechAsync("Unknown command.", cancellationToken);
    }

    private async Task HelpAsync(CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLineLinux("You are in analysis mode. Say \"list commands\" to hear available commands, say \"go online\" to resume conversation or \"go offline\" to pause transcription.");
        await SendReplyWithSpeechAsync(sb.ToString(), $"diagnostics_{Guid.NewGuid()}", false, cancellationToken);
    }

    private async Task ListCommandsAsync(CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLineLinux("You can use the following commands:");
        sb.AppendLineLinux("repeat");
        sb.AppendLineLinux("list services");
        sb.AppendLineLinux("list commands");
        sb.AppendLineLinux("reset chat");
        sb.AppendLineLinux("regenerate");
        sb.AppendLineLinux("rollback");
        await SendReplyWithSpeechAsync(sb.ToString(), $"diagnostics_{Guid.NewGuid()}", false, cancellationToken);
    }

    private async Task ListServicesAsync(CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLineLinux("Diagnostics for character " + _chatSessionData.Character.Name);
        sb.AppendLineLinux("Text Generation: " + _textGen.SettingsRef.ServiceName);
        sb.AppendLineLinux("Text To Speech: " + (_speechGenerator.Link?.ServiceName ?? "None") + " with voice " + _speechGenerator.Voice);
        sb.AppendLineLinux("Speech To Text: " + (_speechToText?.SettingsRef.ServiceName ?? "None"));
        sb.AppendLineLinux("Action Inference: " + (_actionInference?.SettingsRef.ServiceName ?? "None"));
        sb.AppendLineLinux("Summarization: " + (_summarizationService?.SettingsRef.ServiceName ?? "None"));
        await SendReplyWithSpeechAsync(sb.ToString(), $"diagnostics_{Guid.NewGuid()}", false, cancellationToken);
    }

    private async Task ResetChatAsync(CancellationToken cancellationToken)
    {
        _chatSessionData.Actions = null;
        _chatSessionData.Context = null;
        using (var token = _chatSessionData.GetWriteToken())
        {
            token.Memories.Clear();
            token.Messages.Clear();
        }
        _chatSessionState.State = ChatSessionStates.Live;
        await SendFirstMessageAsync(cancellationToken);
    }

    private async Task RepeatAsync(string text, CancellationToken cancellationToken)
    {
        await SendReplyWithSpeechAsync(text[7..], $"diag_{Guid.NewGuid()}", false, cancellationToken);
    }

    private async Task RegenerateAsync(CancellationToken cancellationToken)
    {
        var (_, lastMessage) = DeleteLastExchange();

        if (lastMessage != null)
        {
            _chatSessionState.State = ChatSessionStates.Live;
            HandleClientMessage(new ClientSendMessage
            {
                Text = lastMessage.Value,
            });
        }
        else
        {
            await SendReplyWithSpeechAsync("No messages found to regenerate from.", $"diag_{Guid.NewGuid()}", false, cancellationToken);
        }
    }

    private async Task RollbackAsync(CancellationToken cancellationToken)
    {
        var (deletedMessages, _) = DeleteLastExchange();

        await SendReplyWithSpeechAsync($"Deleted last {deletedMessages} messages", $"diag_{Guid.NewGuid()}", false, cancellationToken);
    }

    private (int, ChatMessageData?) DeleteLastExchange()
    {
        int deletedMessages;
        ChatMessageData? lastMessage = null;
        using (var token = _chatSessionData.GetWriteToken())
        {
            var messagesCountBefore = token.Messages.Count;

            // Remove all last messages from Character
            while (token.Messages.Count > 0 && token.Messages.Last().Role == ChatMessageRole.Assistant)
                token.Messages.RemoveAt(token.Messages.Count - 1);

            // Remove all last messages from User
            while (token.Messages.Count > 0 && token.Messages.Last().Role == ChatMessageRole.User)
            {
                if (lastMessage == null) lastMessage = token.Messages.Last();
                token.Messages.RemoveAt(token.Messages.Count - 1);
            }

            deletedMessages = messagesCountBefore - token.Messages.Count;
        }

        return (deletedMessages, lastMessage);
    }
}