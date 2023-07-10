using System.Net;
using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Services;
using ChatMate.Core;
using ChatMate.Server.Chat;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers;

[ApiController]
public class SpeechController : ControllerBase
{
    [HttpGet("/tts/gens/{id}.{extension}")]
    public async Task GetSpeech(
        [FromRoute] string id,
        // ReSharper disable once UnusedParameter.Global
        [FromRoute] string extension,
        [FromServices] IServiceFactory<ITextToSpeechService> speechGenFactory,
        [FromServices] PendingSpeechManager pendingSpeech,
        [FromServices] IAudioConverter audioConverter,
        CancellationToken cancellationToken
    )
    {
        if (!pendingSpeech.TryGetValue(id, out var speechRequest))
        {
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            Response.ContentType = "text/plain";
            await Response.WriteAsync($"No pending speech for {id}", cancellationToken);
            return;
        }

        var textToSpeech = await speechGenFactory.CreateAsync(speechRequest.Service, cancellationToken);
        audioConverter.SelectContentType(new[] { speechRequest.ContentType }, textToSpeech.ContentType);
        var speechTunnel = new ConversionSpeechTunnel(new HttpResponseSpeechTunnel(Response), audioConverter);
        await textToSpeech.GenerateSpeechAsync(speechRequest, speechTunnel, cancellationToken);
    }
    

    [HttpGet("/tts/services/{service}/voices")]
    public async Task<VoiceInfo[]> GetSpeech(
        [FromRoute] string service,
        [FromServices] IServiceFactory<ITextToSpeechService> speechGenFactory,
        CancellationToken cancellationToken
    )
    {
        var textToSpeech = await speechGenFactory.CreateAsync(service, cancellationToken);
        return await textToSpeech.GetVoicesAsync(cancellationToken);
    }
}