using System.Net;
using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Services;
using Voxta.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Voxta.Host.AspNetCore.WebSockets.Controllers;

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
        audioConverter.SelectOutputContentType(new[] { speechRequest.ContentType }, textToSpeech.ContentType);
        if (speechRequest.Reusable)
        {
            // Get the local low user directory
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var file = Path.Combine(path, $"{id}.{AudioData.GetExtension(audioConverter.ContentType)}");
            if (!System.IO.File.Exists(file))
            {
                var fileTunnel = new ConversionSpeechTunnel(new FileSpeechTunnel(file), audioConverter);
                await textToSpeech.GenerateSpeechAsync(speechRequest, fileTunnel, cancellationToken);
            }
            await Response.SendFileAsync(file, cancellationToken: cancellationToken);
        }
        else
        {
            ISpeechTunnel speechTunnel = new ConversionSpeechTunnel(new HttpResponseSpeechTunnel(Response), audioConverter);
            await textToSpeech.GenerateSpeechAsync(speechRequest, speechTunnel, cancellationToken);
        }
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