using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface ITextGenService : ITokenCounter, IService
{
    (List<ChatMessageData> Messages, int Tokens)? GetMessagesToSummarize(IChatInferenceData chat);
    ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken);
    ValueTask<string > GenerateAsync(string prompt, CancellationToken cancellationToken);
}