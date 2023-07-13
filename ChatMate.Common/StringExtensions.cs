namespace System;

public static class StringExtensions
{
    public static string TrimExcess(this string value)
    {
        return value.Trim(' ', '\r', '\n');
    }
}