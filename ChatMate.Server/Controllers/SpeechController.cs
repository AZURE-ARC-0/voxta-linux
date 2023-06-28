using System.Net;
using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Core;
using ChatMate.Server.Chat;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers;

[ApiController]
public class SpeechController : ControllerBase
{
    [HttpGet("/tts/{id}.{extension}")]
    public async Task GetSpeech(
        [FromRoute] string id,
        [FromRoute] string extension,
        [FromServices] ISelectorFactory<ITextToSpeechService> speechGenFactory,
        [FromServices] PendingSpeechManager pendingSpeech
    )
    {
        if (!pendingSpeech.TryGetValue(id, out var speechRequest))
        {
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            Response.ContentType = "text/plain";
            await Response.WriteAsync($"No pending speech for {id}");
            return;
        }

        await speechGenFactory.Create(speechRequest.Service).GenerateSpeechAsync(speechRequest, new HttpResponseSpeechTunnel(Response), extension);
    }
}