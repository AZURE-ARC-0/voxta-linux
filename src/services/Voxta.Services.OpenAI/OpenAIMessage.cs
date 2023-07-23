using System.Diagnostics.CodeAnalysis;

namespace Voxta.Services.OpenAI;

[Serializable]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class OpenAIMessage
{
    public required string role { get; set; }
    public required string content { get; set; }
}