using System.Text;

namespace System.Text;

public static class StringBuilderExtensions
{
    public static void AppendLineLinux(this StringBuilder sb, string value)
    {
        sb.Append(value);
        sb.Append('\n');
    }
}