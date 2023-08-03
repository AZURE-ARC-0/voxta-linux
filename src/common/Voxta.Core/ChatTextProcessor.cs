using System.Globalization;
using Humanizer;
using Voxta.Abstractions.Model;

namespace Voxta.Core;

public interface IChatTextProcessor
{
    string ProcessText(string? text, CultureInfo culture);
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

    public string ProcessText(string? text, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("{{now}}", DateTime.Now.Humanize(culture: culture), StringComparison.InvariantCultureIgnoreCase);
        text = text.Replace("{{char}}", _characterName, StringComparison.InvariantCultureIgnoreCase);
        text = text.Replace("{{user}}", _profile.Name, StringComparison.InvariantCultureIgnoreCase);
        text = text.Replace("{{user.description}}", _profile.Description?.Trim(' ', '\r', '\n') ?? "Not specified", StringComparison.InvariantCultureIgnoreCase);
        return text.TrimExcess();
    }
}