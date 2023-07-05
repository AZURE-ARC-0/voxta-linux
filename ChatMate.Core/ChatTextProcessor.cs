﻿using System.Globalization;
using ChatMate.Abstractions.Model;

namespace ChatMate.Core;

public class ChatTextProcessor
{
    private readonly ProfileSettings _profile;
    private readonly string _botName;

    public ChatTextProcessor(ProfileSettings profile, string botName)
    {
        _profile = profile;
        _botName = botName;
    }

    public string ProcessText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("{{Now}}", DateTime.Now.ToString("f", CultureInfo.InvariantCulture));
        text = text.Replace("{{Bot}}", _botName);
        text = text.Replace("{{User}}", _profile.Name);
        text = text.Replace("{{UserDescription}}", _profile.Description?.Trim(' ', '\r', '\n') ?? "Not specified");
        return text.Trim(' ', '\r', '\n');
    }
}