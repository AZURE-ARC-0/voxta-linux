namespace Voxta.Abstractions.System;

public interface ILocalEncryptionProvider
{
    string Encrypt(string value);
    string Decrypt(string value);
}

public class NullEncryptionProvider : ILocalEncryptionProvider
{
    public string Encrypt(string value)
    {
        return value;
    }

    public string Decrypt(string value)
    {
        return value;
    }
}

public static class LocalEncryptionProviderExtensions
{
    public static string SafeDecrypt(this ILocalEncryptionProvider provider, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        
        try
        {
            return provider.Decrypt(value);
        }
        catch (Exception exc)
        {
            return "Failed to decrypt: " + exc.Message;
        }
    } 
}