using System.Net;
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

    public async Task SendAsync(byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        _response.StatusCode = (int)HttpStatusCode.OK;
        _response.ContentType = contentType;
        _response.Headers.ContentDisposition = "attachment";
        _response.ContentLength = bytes.Length;
        await _response.BodyWriter.WriteAsync(bytes, cancellationToken);
    }

    public string? GetPath()
    {
        return null;
    }
}