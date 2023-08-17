using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Repositories;

namespace Voxta.Server.Controllers;

[Controller]
public class TalkController : Controller
{
    private readonly IProfileRepository _profileRepository;

    public TalkController(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }
    
    [HttpGet("/talk")]
    public async Task<IActionResult> Talk(CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        if (profile == null) return RedirectToAction("ProfileSettings", "Settings");
        return View();
    }
}