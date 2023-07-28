using Microsoft.AspNetCore.Mvc;

namespace Voxta.Server.Controllers;

[Controller]
public class PlaygroundController : Controller
{
    [HttpGet("/playground/text-to-speech")]
    public ActionResult TextToSpeech()
    {
        return View();
    }
}