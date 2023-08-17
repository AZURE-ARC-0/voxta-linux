namespace Voxta.Abstractions;

public interface IPrerequisitesValidator
{
    public bool ValidateFeatures(params string[] features);
    public bool ValidateCulture(params string[] cultures);
}
