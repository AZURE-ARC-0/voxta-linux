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
            ? RedirectToAction("ProfileSettings", "Settings")
            : RedirectToAction("Talk", "Talk");
    }
    
    [HttpGet("/support")]
    public ActionResult Support()
    {
        return View();
    }
    
    [HttpGet("/safety")]
    public ActionResult Safety()
    {
        return View();
    }
}