using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Common;
using ChatMate.Server.ViewModels;
using ChatMate.Services.KoboldAI;
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
    public async Task<IActionResult> Settings([FromServices] ISettingsRepository settingsRepository, [FromServices] IProfileRepository profileRepository, CancellationToken cancellationToken)
    {
        var openai = await settingsRepository.GetAsync<OpenAISettings>(OpenAIConstants.ServiceName, cancellationToken);
        var novelai = await settingsRepository.GetAsync<NovelAISettings>(NovelAIConstants.ServiceName, cancellationToken);
        var koboldai = await settingsRepository.GetAsync<KoboldAISettings>(KoboldAIConstants.ServiceName, cancellationToken);
        var profile = await profileRepository.GetProfileAsync(cancellationToken);

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
            KoboldAI = new KoboldAISettings
            {
                Uri = koboldai?.Uri ?? "http://localhost:5001",
            },
            Profile = new ProfileSettings
            {
                Name = profile?.Name ?? "User",
                Description = profile?.Description ?? "",
                EnableSpeechRecognition = profile?.EnableSpeechRecognition ?? true,
                PauseSpeechRecognitionDuringPlayback = profile?.PauseSpeechRecognitionDuringPlayback ?? true,
                AnimationSelectionService = profile != null ? profile.AnimationSelectionService : OpenAIConstants.ServiceName,
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
        
        await settingsRepository.SaveAsync(OpenAIConstants.ServiceName, new OpenAISettings
        {
            ApiKey = string.IsNullOrEmpty(model.OpenAI.ApiKey) ? "" : Crypto.EncryptString(model.OpenAI.ApiKey.Trim('"', ' ')),
            Model = model.OpenAI.Model.Trim(),
        });
        await settingsRepository.SaveAsync(NovelAIConstants.ServiceName, new NovelAISettings
        {
            Token = string.IsNullOrEmpty(model.NovelAI.Token) ? "" : Crypto.EncryptString(model.NovelAI.Token.Trim('"', ' ')),
            Model = model.NovelAI.Model.Trim(),
        });
        await settingsRepository.SaveAsync(KoboldAIConstants.ServiceName, new KoboldAISettings
        {
            Uri = model.KoboldAI.Uri,
        });
        await profileRepository.SaveProfileAsync(new ProfileSettings
        {
            Name = model.Profile.Name.Trim(),
            Description = model.Profile.Description?.Trim(),
            EnableSpeechRecognition = model.Profile.EnableSpeechRecognition,
            PauseSpeechRecognitionDuringPlayback = model.Profile.PauseSpeechRecognitionDuringPlayback,
            AnimationSelectionService = model.Profile.AnimationSelectionService,
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
                Postamble = "",
                Greeting = "Hi!",
                SampleMessages = Array.Empty<BotDefinition.Message>(),
                Services = new BotDefinition.ServicesMap
                {
                    TextGen = new BotDefinition.ServiceMap
                    {
                        Service = OpenAIConstants.ServiceName,
                    },
                    SpeechGen = new BotDefinition.VoiceServiceMap
                    {
                        Service = NovelAIConstants.ServiceName,
                        Voice = "Naia",
                    },
                },
                Options = new BotDefinition.BotOptions
                {
                    EnableThinkingSpeech = true,
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