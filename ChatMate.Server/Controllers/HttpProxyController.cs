using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server;

[ApiController]
public class HttpProxyController : ControllerBase
{
    [HttpGet("/speech/{id}.mp3")]
    public async Task GetSpeech([FromRoute] Guid id, [FromServices] ITextToSpeechService speechGen)
    {
        await speechGen.HandleSpeechProxyRequestAsync(Response, id);
    }
}