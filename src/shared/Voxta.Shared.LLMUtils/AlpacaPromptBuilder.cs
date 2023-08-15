using System.Text;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LLMUtils;

public class AlpacaPromptBuilder : TextPromptBuilder
{
    public AlpacaPromptBuilder(ITokenizer tokenizer) : base(tokenizer)
    {
    }

    public AlpacaPromptBuilder(ITokenizer tokenizer, ITimeProvider timeProvider) : base(tokenizer, timeProvider)
    {
    }

    protected override void BeginMessages(StringBuilder sb)
    {
        sb.AppendLineLinux("### Instruction:");
    }

    protected override void EndMessages(StringBuilder sb, string query)
    {
        sb.AppendLineLinux();
        sb.AppendLineLinux("### Response:");
        sb.Append(query);
    }

    protected override void FormatMessage(StringBuilder sb, IChatInferenceData chat, MessageData message)
    {
        if (message.Role == ChatMessageRole.System)
        {
            sb.AppendLineLinux(message.Value);
            sb.AppendLineLinux();
            sb.AppendLineLinux("### Input:");
        }
        else if (message.Role == ChatMessageRole.User)
        {
            sb.Append(chat.User.Name);
            sb.Append(": ");
            sb.AppendLineLinux(message.Value);
        }
        else if (message.Role == ChatMessageRole.Assistant)
        {
            sb.Append(chat.Character.Name);
            sb.Append(": ");
            sb.AppendLineLinux(message.Value);
        }
        else
        {
            throw new ArgumentOutOfRangeException(null, $"Unknown role: {message.Role}");
        }
    }
}