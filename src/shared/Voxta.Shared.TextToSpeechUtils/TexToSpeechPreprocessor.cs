using System.Globalization;
using System.Text.RegularExpressions;
using Humanizer;

namespace Voxta.Shared.TextToSpeechUtils;

public interface ITextToSpeechPreprocessor
{
    string Preprocess(string text, string culture);
}

public class TextToSpeechPreprocessor : ITextToSpeechPreprocessor
{
    public string Preprocess(string text, string culture)
    {
        var output = text;
        var c = CultureInfo.GetCultureInfo(culture);

        // Case 1: AI in upper case at the end of a word
        output = Regex.Replace(output, @"(\w)AI\b", "$1 AI");

        // Case 2: Transform numbers into words
        output = Regex.Replace(output, @"\b\d+\b", match =>
        {
            var num = Int32.Parse(match.Value);
            return num.ToWords(c);
        });

        // TODO: Add more rules as needed

        return output;
    }
}