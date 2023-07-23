using System.Diagnostics.CodeAnalysis;

namespace ChatMate.Services.OpenAI;

[Serializable]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class OpenAIMessage
{
    public required string role { get; set; }
    public required string content { get; set; }
}