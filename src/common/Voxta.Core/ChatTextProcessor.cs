using System.Globalization;
using Humanizer;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Core;

public class ChatTextProcessor : IChatTextProcessor
{
    private readonly ProfileSettings _profile;
    private readonly string _characterName;
    private readonly CultureInfo _culture;
    private readonly ITokenCounter _tokenCounter;

    public ChatTextProcessor(ProfileSettings profile, string characterName, CultureInfo culture, ITokenCounter tokenCounter)
    {
        _profile = profile;
        _characterName = characterName;
        _culture = culture;
        _tokenCounter = tokenCounter;
    }

    public TextData ProcessText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return TextData.Empty;
        text = text.Replace("{{now}}", DateTime.Now.Humanize(culture: _culture), StringComparison.InvariantCultureIgnoreCase);
        text = text.Replace("{{char}}", _characterName, StringComparison.InvariantCultureIgnoreCase);
        text = text.Replace("{{user}}", _profile.Name, StringComparison.InvariantCultureIgnoreCase);
        text = text.Replace("{{user.description}}", _profile.Description?.Trim(' ', '\r', '\n') ?? "Not specified", StringComparison.InvariantCultureIgnoreCase);
        var result = text.TrimExcess();
        var tokens = _tokenCounter.GetTokenCount(result);
        return new TextData
        {
            Value = result,
            Tokens = tokens
        };
    }
}