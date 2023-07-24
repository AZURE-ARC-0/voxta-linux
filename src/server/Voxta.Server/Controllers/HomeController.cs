using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Repositories;

namespace Voxta.Server.Controllers;

[Controller]
public class HomeController : Controller
{
    [HttpGet("/")]
    public async Task<ActionResult> Index([FromServices] IProfileRepository profileRepository, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetProfileAsync(cancellationToken);
        return profile == null
            ? RedirectToAction("Settings", "Settings")
            : RedirectToAction("Chat", "Chat");
    }
    
    [HttpGet("/support")]
    public ActionResult Support()
    {
        return View();
    }
}