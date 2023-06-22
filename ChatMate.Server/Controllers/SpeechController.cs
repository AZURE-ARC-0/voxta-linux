using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server;

[ApiController]
public class SpeechController : ControllerBase
{
    [HttpGet("/chats/{chatId}/messages/{messageId}/speech/{filename}.{extension}")]
    public async Task GetSpeech(
        [FromRoute] Guid chatId,
        [FromRoute] Guid messageId,
        [FromRoute] string filename,
        [FromRoute] string extension,
        [FromServices] SelectorFactory<ITextToSpeechService> speechGenFactory,
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

        await speechGenFactory.Create(speechRequest.Service).GenerateSpeechAsync(speechRequest, Response, extension);
    }
}