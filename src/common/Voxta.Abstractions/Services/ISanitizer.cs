namespace Voxta.Abstractions.Services;

public interface ISanitizer
{
    string Sanitize(string message);
    string StripUnfinishedSentence(string result);
}