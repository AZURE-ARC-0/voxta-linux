using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers;

[Controller]
public class ChatController : Controller
{
    [HttpGet("/chat")]
    public IActionResult Chat()
    {
        return View();
    }
    
    [HttpGet("/newchat")]
    public IActionResult NewChat()
    {
        return View();
    }
}