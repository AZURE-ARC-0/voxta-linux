using System.Net;
using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Services;
using Voxta.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voxta.Common;

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
        
        if (string.IsNullOrEmpty(speechRequest.Service)) throw new InvalidOperationException("TTS service must be resolved prior to adding to pending speech.");

        // NOTE: Here we don't specify prerequisites because it's too late anyway.
        var textToSpeech = await speechGenFactory.CreateAsync(ServicesList.For(speechRequest.Service), speechRequest.Service, Array.Empty<string>(), speechRequest.Culture, cancellationToken);
        audioConverter.SelectOutputContentType(new[] { speechRequest.ContentType }, textToSpeech.ContentType);
        if (speechRequest.Reusable)
        {
            await WriteConvertedFile(id, audioConverter, fileTunnel => textToSpeech.GenerateSpeechAsync(speechRequest, fileTunnel, cancellationToken), cancellationToken);
        }
        else
        {
            pendingSpeech.RemoveValue(id);
            ISpeechTunnel speechTunnel = new ConversionSpeechTunnel(new HttpResponseSpeechTunnel(Response), audioConverter);
            await textToSpeech.GenerateSpeechAsync(speechRequest, speechTunnel, cancellationToken);
        }
    }


    [HttpGet("/tts/file")]
    public async Task GetFileSpeech(
        [FromQuery] string path,
        [FromQuery] string contentType,
        [FromServices] IAudioConverter audioConverter,
        CancellationToken cancellationToken
    )
    {
        // check if path is inside the audio path
        var audioPath = Path.GetFullPath("Data/Audio");
        var fullPath = Path.GetFullPath(Path.Combine(audioPath, path));
        if (!fullPath.StartsWith(audioPath) || !System.IO.File.Exists(fullPath))
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            Response.ContentType = "text/plain";
            await Response.WriteAsync($"Path '{path}' was not found", cancellationToken);
            return;
        }

        audioConverter.SelectOutputContentType(new[] { contentType }, AudioData.FromExtension(Path.GetExtension(path)));
        await WriteConvertedFile(Crypto.CreateSha1Hash(path), audioConverter, fileTunnel => fileTunnel.SendAsync(new AudioData(System.IO.File.OpenRead(fullPath), audioConverter.ContentType), cancellationToken), cancellationToken);
    }

    private async Task WriteConvertedFile(string id, IAudioConverter audioConverter, Func<ISpeechTunnel, Task> generateSpeechAsync, CancellationToken cancellationToken)
    {
        // Get the local low user directory
        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var file = Path.Combine(path, $"{id}.{AudioData.GetExtension(audioConverter.ContentType)}");
        if (!System.IO.File.Exists(file))
        {
            var fileTunnel = new ConversionSpeechTunnel(new FileSpeechTunnel(file), audioConverter);
            await generateSpeechAsync(fileTunnel);
        }
        Response.StatusCode = (int)HttpStatusCode.OK;
        Response.ContentType = audioConverter.ContentType;
        await Response.SendFileAsync(file, cancellationToken: cancellationToken);
    }

    [HttpGet("/tts/services/{service}/voices")]
    public async Task<VoiceInfo[]> GetVoices(
        [FromRoute] string service,
        [FromQuery] string culture,
        [FromServices] IServiceFactory<ITextToSpeechService> speechGenFactory,
        CancellationToken cancellationToken
    )
    {
        if (service == "None")
        {
            return new VoiceInfo[]
            {
                new() { Id = "", Label = "Unspecified" },
                new() { Id = SpecialVoices.Male, Label = "Male" },
                new() { Id = SpecialVoices.Female, Label = "Female" },
            };
        }
        // NOTE: There is no voices list implementation that require any prerequisites.
        var textToSpeech = await speechGenFactory.CreateAsync(ServicesList.For(service), service, Array.Empty<string>(), culture, cancellationToken);
        return await textToSpeech.GetVoicesAsync(cancellationToken);
    }
}