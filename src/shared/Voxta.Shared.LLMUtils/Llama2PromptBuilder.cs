using System.Text;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LLMUtils;

public class Llama2PromptBuilder : TextPromptBuilder
{
    public Llama2PromptBuilder(ITokenizer tokenizer) : base(tokenizer)
    {
    }

    public Llama2PromptBuilder(ITokenizer tokenizer, ITimeProvider timeProvider) : base(tokenizer, timeProvider)
    {
    }

    protected override void BeginMessages(StringBuilder sb)
    {
    }

    protected override void EndMessages(StringBuilder sb, string query)
    {
    }

    protected override void FormatMessage(StringBuilder sb, IChatInferenceData chat, MessageData message)
    {
        if (message.Role == ChatMessageRole.System)
        {
            sb.AppendLineLinux("<s>[INST] <<SYS>>");
            sb.AppendLineLinux(message.Value);
            sb.AppendLineLinux("<</SYS>>");
            sb.AppendLineLinux();
            sb.Append("[/INST]");
        }
        else if (message.Role == ChatMessageRole.User)
        {
            sb.Append("[INST] ");
            sb.Append(message.Value);
            sb.Append(" [/INST]");
        }
        else if (message.Role == ChatMessageRole.Assistant)
        {
            sb.Append(' ');
            sb.Append(message.Value);
            sb.Append(" </s><s>");
        }
        else
        {
            throw new ArgumentOutOfRangeException(null, $"Unknown role: {message.Role}");
        }
    }
}