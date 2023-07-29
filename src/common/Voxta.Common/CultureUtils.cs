using System.Globalization;

namespace Voxta.Common;

public abstract class CultureUtils
{
    public static readonly (string Name, string Label)[] Bcp47LanguageTags = CultureInfo.GetCultures(CultureTypes.AllCultures)
        .Select(c => (c.IetfLanguageTag, c.DisplayName))
        .ToArray();
}