using System.Text.RegularExpressions;

namespace ChatMate.Common;

public class Sanitizer
{
    private static readonly Regex RemoveNonChat = new (@"\*[^*]+\*", RegexOptions.Compiled);
    private static readonly Regex SanitizeMessage = new(@"[^a-zA-Z0-9 '""\-\.\!\?\,\;]", RegexOptions.Compiled);

    public string Sanitize(string message)
    {
        var result = message;
        result = RemoveNonChat.Replace(result, "");
        result = SanitizeMessage.Replace(result, "");
        return result.Trim('\"', '\'', ' ');
    }
}