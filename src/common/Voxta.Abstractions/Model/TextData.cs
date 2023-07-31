namespace Voxta.Abstractions.Model;

[Serializable]
public class TextData
{
    public required string Text { get; set; }
    public int Tokens { get; set; }
    public bool HasValue => !string.IsNullOrEmpty(Text);
}