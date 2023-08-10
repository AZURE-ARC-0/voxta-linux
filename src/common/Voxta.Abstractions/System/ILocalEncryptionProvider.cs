namespace Voxta.Abstractions.System;

public interface ILocalEncryptionProvider
{
    string Encrypt(string value);
    string Decrypt(string value);
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