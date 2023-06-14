using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server;

[Controller]
public class HomeController : ControllerBase
{
    [HttpGet("/")]
    public ActionResult Home()
    {
        return Ok("ChatMate Server");
    }
}