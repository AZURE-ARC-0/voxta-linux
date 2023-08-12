using Voxta.Abstractions.System;
#if(WINDOWS)
using System.Security.Cryptography;
#endif

namespace Voxta.Security.Windows;

public class DPAPIEncryptionProvider : ILocalEncryptionProvider
{
    public string Encrypt(string plaintext)
    {
#if(WINDOWS)
        byte[] plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
#else
        return plaintext;
#endif
    }
    
    public string Decrypt(string encrypted)
    {
#if(WINDOWS)
        byte[] encryptedBytes = Convert.FromBase64String(encrypted);
        byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(decryptedBytes);
#else
        return encrypted;
#endif
    }
}