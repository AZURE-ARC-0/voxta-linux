using System.Runtime.ExceptionServices;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;

namespace Voxta.Core;

public class FileSpeechTunnel : ISpeechTunnel
{
    private readonly string _path;

    public FileSpeechTunnel(string path)
    {
        _path = path;
    }
    
    public Task ErrorAsync(Exception exc, CancellationToken cancellationToken)
    {   
        ExceptionDispatchInfo.Capture(exc).Throw();
        throw exc;
    }

    public async Task SendAsync(AudioData audioData, CancellationToken cancellationToken)
    {
        await using var f = File.Create(_path);
        await audioData.Stream.CopyToAsync(f, cancellationToken);
    }
}