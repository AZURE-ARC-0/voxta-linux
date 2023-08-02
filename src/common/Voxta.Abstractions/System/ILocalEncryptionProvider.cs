namespace Voxta.Abstractions.System;

public interface ILocalEncryptionProvider
{
    string Encrypt(string value);
    string Decrypt(string value);
}