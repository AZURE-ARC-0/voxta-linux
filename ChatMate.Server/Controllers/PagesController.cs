using ChatMate.Abstractions.Repositories;
using ChatMate.Server.ViewModels;
using ChatMate.Services.NovelAI;
using ChatMate.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers;

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
    public async Task<IActionResult> Settings([FromServices] ISettingsRepository settingsRepository, [FromServices] IProfileRepository profileRepository)
    {
        var openai = await settingsRepository.GetAsync<OpenAISettings>("OpenAI");
        var novelai = await settingsRepository.GetAsync<NovelAISettings>("NovelAI");
        var profile = await profileRepository.GetProfileAsync();
        
        var vm = new SettingsViewModel
        {
            OpenAI = openai,
            NovelAI = novelai,
            Profile = profile
        };
        
        return View(vm);
    }
    
    [HttpPost("/settings")]
    public async Task<IActionResult> PostSettings([FromForm] SettingsViewModel model, [FromServices] ISettingsRepository settingsRepository, [FromServices] IProfileRepository profileRepository)
    {
        if (!ModelState.IsValid)
        {
            return View("Settings", model);
        }
        
        await settingsRepository.SaveAsync("OpenAI", model.OpenAI);
        await settingsRepository.SaveAsync("NovelAI", model.NovelAI);
        await profileRepository.SaveProfileAsync(model.Profile);
        
        return RedirectToAction("Chat");
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