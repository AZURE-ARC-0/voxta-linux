using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers.Pages;

[Controller]
public class DiagnosticsController : Controller
{
    [HttpGet("/diagnostics")]
    public IActionResult Diagnostics()
    {
        return View();
    }
}