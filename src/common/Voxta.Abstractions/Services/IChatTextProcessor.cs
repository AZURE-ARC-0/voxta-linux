using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IChatTextProcessor
{
    TextData ProcessText(string? text);
}