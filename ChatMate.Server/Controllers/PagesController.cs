using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Common;
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
        var openai = await settingsRepository.GetAsync<OpenAISettings>("OpenAI") ?? new OpenAISettings { ApiKey = "" };
        openai.ApiKey = string.IsNullOrEmpty(openai.ApiKey) ? "" : Crypto.DecryptString(openai.ApiKey);
        var novelai = await settingsRepository.GetAsync<NovelAISettings>("NovelAI") ?? new NovelAISettings { Token = "" };
        novelai.Token = string.IsNullOrEmpty(novelai.Token) ? "" : Crypto.DecryptString(novelai.Token);
        var profile = await profileRepository.GetProfileAsync() ?? new ProfileSettings { Name = "User", Description = "" };
        
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
        
        model.OpenAI.ApiKey = string.IsNullOrEmpty(model.OpenAI.ApiKey) ? "" : Crypto.EncryptString(model.OpenAI.ApiKey);
        await settingsRepository.SaveAsync("OpenAI", model.OpenAI);
        model.NovelAI.Token = string.IsNullOrEmpty(model.NovelAI.Token) ? "" : Crypto.EncryptString(model.NovelAI.Token);
        await settingsRepository.SaveAsync("NovelAI", model.NovelAI);
        await profileRepository.SaveProfileAsync(model.Profile);
        
        return RedirectToAction("Chat");
    }
    
    [HttpGet("/bots")]
    public async Task<IActionResult> Bots([FromServices] IBotRepository botRepository, CancellationToken cancellationToken)
    {
        var model = await botRepository.GetBotsListAsync(cancellationToken);
        return View(model);
    }
    
    [HttpGet("/bots/{botId}")]
    public IActionResult Bot()
    {
        return View();
    }
}