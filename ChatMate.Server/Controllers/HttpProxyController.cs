using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server;

[ApiController]
public class HttpProxyController : ControllerBase
{
    [HttpGet("/speech/{id}.{extension}")]
    public async Task GetSpeech([FromRoute] Guid id, [FromRoute] string extension, [FromServices] ITextToSpeechService speechGen)
    {
        await speechGen.HandleSpeechProxyRequestAsync(Response, id, extension);
    }
}