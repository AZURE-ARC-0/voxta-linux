using System.Globalization;
using Voxta.Abstractions.Model;

namespace Voxta.Core;

public interface IChatTextProcessor
{
    string ProcessText(string? text);
}

public class ChatTextProcessor : IChatTextProcessor
{
    private readonly ProfileSettings _profile;
    private readonly string _characterName;

    public ChatTextProcessor(ProfileSettings profile, string characterName)
    {
        _profile = profile;
        _characterName = characterName;
    }

    public string ProcessText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("{{Now}}", DateTime.Now.ToString("f", CultureInfo.InvariantCulture));
        text = text.Replace("{{char}}", _characterName);
        text = text.Replace("{{user}}", _profile.Name);
        text = text.Replace("{{UserDescription}}", _profile.Description?.Trim(' ', '\r', '\n') ?? "Not specified");
        return text.TrimExcess();
    }
}