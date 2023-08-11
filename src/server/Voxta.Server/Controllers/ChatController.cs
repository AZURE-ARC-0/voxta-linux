using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Repositories;

namespace Voxta.Server.Controllers;

[Controller]
public class ChatController : Controller
{
    private readonly IProfileRepository _profileRepository;

    public ChatController(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }
    
    [HttpGet("/chat")]
    public async Task<IActionResult> Chat(CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken);
        if (profile == null) return RedirectToAction("ProfileSettings", "Settings");
        return View();
    }
}