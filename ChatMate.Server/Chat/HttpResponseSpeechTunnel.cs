using System.Net;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;

namespace ChatMate.Server.Chat;

public class HttpResponseSpeechTunnel : ISpeechTunnel
{
    private readonly HttpResponse _response;

    public HttpResponseSpeechTunnel(HttpResponse response)
    {
        _response = response;
    }
    
    public async Task ErrorAsync(string message, CancellationToken cancellationToken)
    {
        _response.StatusCode = (int)HttpStatusCode.InternalServerError;
        _response.ContentType = "text/plain";
        await _response.WriteAsync(message, cancellationToken: cancellationToken);
    }

    public async Task SendAsync(AudioData audioData, CancellationToken cancellationToken)
    {
        _response.StatusCode = (int)HttpStatusCode.OK;
        _response.ContentType = audioData.ContentType;
        _response.Headers.ContentDisposition = "attachment";
        audioData.Stream.Seek(0, SeekOrigin.Begin);
        _response.ContentLength = audioData.Stream.Length - audioData.Stream.Position;
        await audioData.Stream.CopyToAsync(_response.Body, cancellationToken);
    }
}