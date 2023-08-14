using System.Text.RegularExpressions;
using Voxta.Abstractions.Services;

namespace Voxta.Core;

public class Sanitizer : ISanitizer
{
    // Wow! *looking down* That was not a spoken word.
    private static readonly Regex RemoveNonChat = new (@"\*[^*]+\*", RegexOptions.Compiled);
    // (smiling) I did not say that.
    private static readonly Regex RemoveProbableEmotes = new (@"\([^)]+\)", RegexOptions.Compiled);
    // Removes anything that doesn't look like a word or a punctuation.
    private static readonly Regex SanitizeMessage = new(@"[^a-zA-Z0-9 '""()\-.!?,;:0-9A-Za-z\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u02af\u1d00-\u1d25\u1d62-\u1d65\u1d6b-\u1d77\u1d79-\u1d9a\u1e00-\u1eff\u2090-\u2094\u2184-\u2184\u2488-\u2490\u271d-\u271d\u2c60-\u2c7c\u2c7e-\u2c7f\ua722-\ua76f\ua771-\ua787\ua78b-\ua78c\ua7fb-\ua7ff\ufb00-\ufb06]", RegexOptions.Compiled);
    private readonly char[] _punctuation = { '.', '!', '?' };

    public string Sanitize(string message)
    {
        var result = message;
        if (result.StartsWith("1) ")) result = result[3..];
        if (result.StartsWith("- ")) result = result[2..];
        result = RemoveNonChat.Replace(result, "");
        result = RemoveProbableEmotes.Replace(result, "");
        result = SanitizeMessage.Replace(result, "");
        result = result.Trim('\"', '\'', ' ');
        if (result == "") return "...";

        result = StripUnfinishedSentence(result);

        return result;
    }

    public string StripUnfinishedSentence(string result)
    {
        var lastPunctuationIndex = result.LastIndexOfAny(_punctuation);

        if (lastPunctuationIndex != -1 && lastPunctuationIndex != result.Length - 1)
        {
            result = result[..(lastPunctuationIndex + 1)];
        }

        return result;
    }
}