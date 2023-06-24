using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server;

[Controller]
public class PagesController : Controller
{
    [HttpGet("/")]
    public ActionResult Index()
    {
        return View();
    }
    
    [HttpGet("/chat")]
    public IActionResult Chat()
    {
        return View();
    }
    
    [HttpGet("/settings")]
    public IActionResult Settings()
    {
        return View();
    }
    
    [HttpGet("/bots")]
    public IActionResult Bots()
    {
        return View();
    }
    
    [HttpGet("/bots/{botId}")]
    public IActionResult Bot()
    {
        return View();
    }
}