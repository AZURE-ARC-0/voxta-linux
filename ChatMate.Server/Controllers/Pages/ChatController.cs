using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers.Pages;

[Controller]
public class ChatController : Controller
{
    [HttpGet("/chat")]
    public IActionResult Chat()
    {
        return View();
    }
}