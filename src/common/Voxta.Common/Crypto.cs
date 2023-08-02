#pragma warning disable CA1416
using System.Security.Cryptography;

namespace Voxta.Common;

public static class Crypto
{
    public static Guid CreateCryptographicallySecureGuid()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return new Guid(bytes);
    }

    public static string CreateSha1Hash(string value)
    {
        var hash = SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}