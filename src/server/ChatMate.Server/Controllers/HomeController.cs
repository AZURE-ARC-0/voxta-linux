using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers;

[Controller]
public class HomeController : Controller
{
    [HttpGet("/")]
    public ActionResult Index()
    {
        return View();
    }
}