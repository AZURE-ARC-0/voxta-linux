#pragma warning disable CA1416
using System.Security.Cryptography;

namespace ChatMate.Common;

public static class Crypto
{
    public static Guid CreateCryptographicallySecureGuid()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return new Guid(bytes);
    }
    
    public static string EncryptString(string plaintext)
    {
        byte[] plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }
    
    public static string DecryptString(string encrypted)
    {
        byte[] encryptedBytes = Convert.FromBase64String(encrypted);
        byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(decryptedBytes);
    }
}