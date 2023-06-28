using ChatMate.Abstractions.Network;

namespace ChatMate.Core;

public class FileSpeechTunnel : ISpeechTunnel
{
    private readonly string _path;

    public FileSpeechTunnel(string path)
    {
        _path = path;
    }
    
    public Task ErrorAsync(string message)
    {
        throw new Exception(message);
    }

    public Task SendAsync(byte[] bytes, string contentType)
    {
        return File.WriteAllBytesAsync(_path, bytes);
    }
}