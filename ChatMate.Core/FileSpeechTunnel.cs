using ChatMate.Abstractions.Model;
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

    public async Task SendAsync(AudioData audioData, CancellationToken cancellationToken)
    {
        await using var f = File.Create(_path);
        await audioData.Stream.CopyToAsync(f, cancellationToken);
    }
}