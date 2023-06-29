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
        var openai = await settingsRepository.GetAsync<OpenAISettings>("OpenAI");
        var novelai = await settingsRepository.GetAsync<NovelAISettings>("NovelAI");
        var profile = await profileRepository.GetProfileAsync();

        var vm = new SettingsViewModel
        {
            OpenAI = new OpenAISettings
            {
                ApiKey = string.IsNullOrEmpty(openai?.ApiKey) ? "" : Crypto.DecryptString(openai.ApiKey),
                Model = openai?.Model ?? OpenAISettings.DefaultModel,
            },
            NovelAI = new NovelAISettings
            {
                Token = string.IsNullOrEmpty(novelai?.Token) ? "" : Crypto.DecryptString(novelai.Token),
                Model = novelai?.Model ?? NovelAISettings.DefaultModel,
            },
            Profile = new ProfileSettings
            {
                Name = profile?.Name ?? "User",
                Description = profile?.Description ?? ""
            }
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
        
        await settingsRepository.SaveAsync("OpenAI", new OpenAISettings
        {
            ApiKey = string.IsNullOrEmpty(model.OpenAI.ApiKey) ? "" : Crypto.EncryptString(model.OpenAI.ApiKey.Trim('"', ' ')),
            Model = model.OpenAI.Model.Trim(),
        });
        await settingsRepository.SaveAsync("NovelAI", new NovelAISettings
        {
            Token = string.IsNullOrEmpty(model.NovelAI.Token) ? "" : Crypto.EncryptString(model.NovelAI.Token.Trim('"', ' ')),
            Model = model.NovelAI.Model.Trim(),
        });
        await profileRepository.SaveProfileAsync(new ProfileSettings
        {
            Name = model.Profile.Name.Trim(),
            Description = model.Profile.Description?.Trim(),
        });
        
        return RedirectToAction("Chat");
    }
    
    [HttpGet("/bots")]
    public async Task<IActionResult> Bots([FromServices] IBotRepository botRepository, CancellationToken cancellationToken)
    {
        var model = await botRepository.GetBotsListAsync(cancellationToken);
        return View(model);
    }
    
    [HttpGet("/bots/{botId}")]
    public async Task<IActionResult> Bot([FromRoute] string botId, [FromServices] IBotRepository botRepository, CancellationToken cancellationToken)
    {
        BotDefinition? bot;
        if (botId == "new")
        {
            bot = new BotDefinition
            {
                Name = "New bot",
                Preamble = "{{Bot}} is a virtual companion for {{User}}.",
                SampleMessages = Array.Empty<BotDefinition.Message>(),
                Services = new BotDefinition.ServicesMap
                {
                    TextGen = new BotDefinition.ServiceMap
                    {
                        Service = "OpenAI",
                        Settings = new Dictionary<string, string>
                        {
                            { "Model", "gpt-3.5-turbo" }
                        }
                    },
                    SpeechGen = new BotDefinition.ServiceMap
                    {
                        Service = "NovelAI",
                        Settings = new Dictionary<string, string>
                        {
                            { "Voice", "Naia" }
                        }
                    },
                    AnimSelect = new BotDefinition.ServiceMap
                    {
                        Service = "None",
                    }
                }
            };
        }
        else
        {
            bot = await botRepository.GetBotAsync(botId, cancellationToken);
            if (bot == null)
                return NotFound("Bot not found");
        }
            
        return View(bot);
    }
    
    [HttpGet("/diagnostics")]
    public IActionResult Diagnostics()
    {
        return View();
    }
}