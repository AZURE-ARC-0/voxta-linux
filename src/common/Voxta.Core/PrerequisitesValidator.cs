using System.Globalization;
using System.Text;
using Voxta.Abstractions;
using Voxta.Abstractions.Model;

namespace Voxta.Core;

public class PrerequisitesValidator : IPrerequisitesValidator
{
    private readonly string _culture;
    private readonly string[]? _prerequisites;

    public PrerequisitesValidator(CharacterCardExtended character)
    {
        _culture = character.Culture;
        _prerequisites = character.Prerequisites;
    }

    public bool ValidateFeatures(params string[] features)
    {
        if (_prerequisites == null) return true;
        return _prerequisites.All(features.Contains);
    }

    public bool ValidateCulture(params string[] cultures)
    {
        var language = CultureInfo.GetCultureInfoByIetfLanguageTag(_culture).TwoLetterISOLanguageName;
        return cultures.Contains(_culture) || cultures.Any(c => c.StartsWith(language));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("culture: ");
        sb.Append(_culture);
        if (_prerequisites is { Length: > 0 })
        {
            sb.Append(", prerequisites: ");
            sb.Append(string.Join(", ", _prerequisites));
        }
        return sb.ToString();
    }
}

public class IgnorePrerequisitesValidator : IPrerequisitesValidator
{
    public static readonly IPrerequisitesValidator Instance = new IgnorePrerequisitesValidator();
    
    public bool ValidateFeatures(params string[] features)
    {
        return true;
    }

    public bool ValidateCulture(params string[] cultures)
    {
        return true;
    }

    public override string ToString()
    {
        return "(no prerequisites)";
    }
}
