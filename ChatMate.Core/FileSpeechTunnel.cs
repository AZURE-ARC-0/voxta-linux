using ChatMate.Abstractions.Network;

namespace ChatMate.Core;

public class FileSpeechTunnel : ISpeechTunnel
{
    private readonly string _path;

    public FileSpeechTunnel(string path)
    {
        _path = path;
    }
    
    public Task ErrorAsync(string message, CancellationToken cancellationToken)
    {
        throw new Exception(message);
    }

    public Task SendAsync(byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        return File.WriteAllBytesAsync(_path, bytes, cancellationToken);
    }
}