using System.Text.RegularExpressions;

namespace ChatMate.Server;

public class Sanitizer
{
    private static readonly Regex SanitizeMessage = new(@"[^a-zA-Z0-9 '""\-\.\!\?\,\;]", RegexOptions.Compiled);

    public string Sanitize(string message)
    {
        return SanitizeMessage.Replace(message, "").Trim('\"', ' ');
    }
}