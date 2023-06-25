using System.Security.Cryptography;

namespace ChatMate.Common;

public static class Crypto
{
    public static Guid CreateCryptographicallySecureGuid()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return new Guid(bytes);
    }
}