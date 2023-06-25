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
    [HttpGet("/chats/{chatId}/messages/{messageId}/speech/{filename}.{extension}")]
    public async Task GetSpeech(
        [FromRoute] Guid chatId,
        [FromRoute] Guid messageId,
        [FromRoute] string filename,
        [FromRoute] string extension,
        [FromServices] ISelectorFactory<ITextToSpeechService> speechGenFactory,
        [FromServices] PendingSpeechManager pendingSpeech
    )
    {
        if (!pendingSpeech.TryGetValue(chatId, messageId, out var speechRequest))
        {
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            Response.ContentType = "text/plain";
            await Response.WriteAsync($"No pending speech for chat {chatId} and message {messageId}");
            return;
        }

        await speechGenFactory.Create(speechRequest.Service).GenerateSpeechAsync(speechRequest, new HttpResponseSpeechTunnel(Response), extension);
    }
}