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
        else if (text.StartsWith("repeat", StringComparison.InvariantCultureIgnoreCase) && text.Length > 7)
            await RepeatAsync(text, cancellationToken);
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
        await SendReplyWithSpeechAsync(sb.ToString(), $"diagnostics_{Guid.NewGuid()}", false, cancellationToken);
    }

    private async Task ListServicesAsync(CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLineLinux("Diagnostics for character " + _chatSessionData.Character.Name);
        sb.AppendLineLinux("Text Generation: " + _textGen.ServiceName);
        sb.AppendLineLinux("Text To Speech: " + _speechGenerator.ServiceName + " with voice " + _speechGenerator.Voice);
        sb.AppendLineLinux("Action Inference: " + (_actionInference?.ServiceName ?? "None"));
        sb.AppendLineLinux("Speech To Text: " + (_speechToText?.ServiceName ?? "None"));
        await SendReplyWithSpeechAsync(sb.ToString(), $"diagnostics_{Guid.NewGuid()}", false, cancellationToken);
    }

    private async Task RepeatAsync(string text, CancellationToken cancellationToken)
    {
        await SendReplyWithSpeechAsync(text[7..], $"diag_{Guid.NewGuid()}", false, cancellationToken);
    }
}