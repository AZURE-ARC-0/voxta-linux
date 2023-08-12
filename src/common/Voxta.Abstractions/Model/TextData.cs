namespace Voxta.Abstractions.Model;

[Serializable]
public class TextData
{
    public static TextData Empty => new TextData { Text = "", Tokens = 0 };
    
    public required string Text { get; set; }
    public int Tokens { get; set; }
    public bool HasValue => !string.IsNullOrEmpty(Text);

    public override string ToString() => Text;
    
    public static implicit operator TextData(string text) => new() { Text = text };
    public static implicit operator string(TextData text) => text.Text;
}