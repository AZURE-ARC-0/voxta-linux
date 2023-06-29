using System.Globalization;
using ChatMate.Abstractions.Model;

namespace ChatMate.Core;

public class ChatTextProcessor
{
    private readonly BotDefinition _bot;
    private readonly ProfileSettings _profile;

    public ChatTextProcessor(BotDefinition bot, ProfileSettings profile)
    {
        _bot = bot;
        _profile = profile;
    }

    public string ProcessText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("{{Now}}", DateTime.Now.ToString("f", CultureInfo.InvariantCulture));
        text = text.Replace("{{Bot}}", _bot.Name);
        text = text.Replace("{{User}}", _profile.Name);
        text = text.Replace("{{UserDescription}}", _profile.Description?.Trim(' ', '\r', '\n') ?? "Not specified");
        return text.Trim(' ', '\r', '\n');
    }
}