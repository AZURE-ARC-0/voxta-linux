using System.Net;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Microsoft.AspNetCore.Http;

namespace Voxta.Host.AspNetCore.WebSockets;

public class HttpResponseSpeechTunnel : ISpeechTunnel
{
    private readonly HttpResponse _response;

    public HttpResponseSpeechTunnel(HttpResponse response)
    {
        _response = response;
    }
    
    public async Task ErrorAsync(Exception exc, CancellationToken cancellationToken)
    {
        _response.StatusCode = (int)HttpStatusCode.InternalServerError;
        _response.ContentType = "text/plain";
        await _response.WriteAsync(exc.Message, cancellationToken: cancellationToken);
    }

    public async Task SendAsync(AudioData audioData, CancellationToken cancellationToken)
    {
        _response.StatusCode = (int)HttpStatusCode.OK;
        _response.ContentType = audioData.ContentType;
        audioData.Stream.Seek(0, SeekOrigin.Begin);
        _response.ContentLength = audioData.Stream.Length - audioData.Stream.Position;
        await audioData.Stream.CopyToAsync(_response.Body, cancellationToken);
    }
}