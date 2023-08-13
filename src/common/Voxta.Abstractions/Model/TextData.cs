namespace Voxta.Abstractions.Model;

[Serializable]
public class TextData
{
    public static TextData Empty => new() { Value = "", Tokens = 0 };
    
    public required string Value { get; set; }
    public required int Tokens { get; set; }
    public bool HasValue => !string.IsNullOrEmpty(Value);

    public override string ToString() => Value;
    
    public static implicit operator TextData(string text) => new() { Value = text, Tokens = 0 };
    public static implicit operator string(TextData text) => text.Value;
}