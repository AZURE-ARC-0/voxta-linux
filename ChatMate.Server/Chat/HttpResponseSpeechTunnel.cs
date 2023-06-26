﻿using System.Net;
using ChatMate.Abstractions.Network;

namespace ChatMate.Server.Chat;

public class HttpResponseSpeechTunnel : ISpeechTunnel
{
    private readonly HttpResponse _response;

    public HttpResponseSpeechTunnel(HttpResponse response)
    {
        _response = response;
    }
    
    public async Task ErrorAsync(string message)
    {
        _response.StatusCode = (int)HttpStatusCode.InternalServerError;
        _response.ContentType = "text/plain";
        await _response.WriteAsync(message);
    }

    public async Task SendAsync(byte[] bytes, string contentType)
    {
        _response.StatusCode = (int)HttpStatusCode.OK;
        _response.ContentType = contentType;
        _response.Headers.ContentDisposition = "attachment";
        _response.ContentLength = bytes.Length;
        await _response.BodyWriter.WriteAsync(bytes);
    }
}