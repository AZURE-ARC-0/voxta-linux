using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Repositories;
using Voxta.Common;
using Voxta.Server.ViewModels;
using Voxta.Server.ViewModels.Playground;

namespace Voxta.Server.Controllers;

[Controller]
public class PlaygroundController : Controller
{
    [HttpGet("/playground/text-to-speech")]
    public async Task<ActionResult> TextToSpeech([FromServices] IProfileRepository profileRepository, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetProfileAsync(cancellationToken);
        if (profile == null) return RedirectToAction("Settings", "Settings");
        return View(new TextToSpeechPlaygroundViewModel
        {
            Services = profile.TextToSpeech.Services.Select(OptionViewModel.Create).ToList(),
            Cultures = CultureUtils.Bcp47LanguageTags.Select(x => new OptionViewModel(x.Name, x.Label)).ToList(),
        });
    }
}