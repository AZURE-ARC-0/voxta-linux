using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server;

public class ChatController : Controller
{
    [HttpGet("/chat")]
    public IActionResult Index()
    {
        return View();
    }
}