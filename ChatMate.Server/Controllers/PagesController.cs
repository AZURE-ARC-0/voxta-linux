using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using ChatMate.Server.ViewModels;
using ChatMate.Services.KoboldAI;
using ChatMate.Services.ElevenLabs;
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
    public async Task<IActionResult> Settings(
        [FromServices] ISettingsRepository settingsRepository,
        [FromServices] IProfileRepository profileRepository,
        CancellationToken cancellationToken)
    {
        var openai = await settingsRepository.GetAsync<OpenAISettings>(OpenAIConstants.ServiceName, cancellationToken);
        var novelai = await settingsRepository.GetAsync<NovelAISettings>(NovelAIConstants.ServiceName, cancellationToken);
        var koboldai = await settingsRepository.GetAsync<KoboldAISettings>(KoboldAIConstants.ServiceName, cancellationToken);
        var elevenlabs = await settingsRepository.GetAsync<ElevenLabsSettings>(ElevenLabsConstants.ServiceName, cancellationToken);
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
            ElevenLabs = new ElevenLabsSettings
            {
              ApiKey  = string.IsNullOrEmpty(elevenlabs?.ApiKey) ? "" : Crypto.DecryptString(elevenlabs.ApiKey),
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
        await settingsRepository.SaveAsync(ElevenLabsConstants.ServiceName, new ElevenLabsSettings
        {
            ApiKey = model.ElevenLabs.ApiKey,
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
    public async Task<IActionResult> Bot(
        [FromRoute] string botId,
        [FromQuery] string? from,
        [FromServices] IBotRepository botRepository,
        [FromServices] IServiceFactory<ITextToSpeechService> ttsServiceFactory,
        CancellationToken cancellationToken
        )
    {
        var isNew = botId == "new";
        BotDefinition? bot;
        if (botId == "new" && from == null)
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
            bot = await botRepository.GetBotAsync(from ?? botId, cancellationToken);
            if (bot == null)
                return NotFound("Bot not found");
            if (isNew) bot.Id = "";
        }

        var vm = await GenerateBotViewModelAsync(ttsServiceFactory, bot, isNew, cancellationToken);

        return View(vm);
    }

    [HttpPost("/bots/{botId}")]
    public async Task<IActionResult> Bot(
        [FromRoute] string botId,
        [FromForm] BotViewModel data,
        [FromServices] IBotRepository botRepository,
        [FromServices] IServiceFactory<ITextToSpeechService> ttsServiceFactory,
        CancellationToken cancellationToken
    )
    {
        if (!ModelState.IsValid)
        {
            var isNew = botId == "new";
            var vm = await GenerateBotViewModelAsync(ttsServiceFactory, data.Bot, isNew, cancellationToken);
            return View(vm);
        }

        if (botId != "new" && botId != data.Bot.Id)
            return BadRequest("Bot ID mismatch");
        await botRepository.SaveBotAsync(data.Bot);
        return RedirectToAction("Bot", new { botId = data.Bot.Id });
    }

    private static async Task<BotViewModelWithOptions> GenerateBotViewModelAsync(IServiceFactory<ITextToSpeechService> ttsServiceFactory, BotDefinition bot, bool isNew,
        CancellationToken cancellationToken)
    {
        var vm = new BotViewModelWithOptions
        {
            IsNew = isNew,
            Bot = bot,
            TextGenServices = new[]
            {
                OpenAIConstants.ServiceName,
                NovelAIConstants.ServiceName,
                KoboldAIConstants.ServiceName,
            },
            TextToSpeechServices = new[]
            {
                NovelAIConstants.ServiceName,
                ElevenLabsConstants.ServiceName,
            },
        };

        if (!string.IsNullOrEmpty(bot.Services.SpeechGen.Service))
        {
            var ttsService = await ttsServiceFactory.CreateAsync(bot.Services.SpeechGen.Service, cancellationToken);
            vm.Voices = await ttsService.GetVoicesAsync(cancellationToken);
        }

        return vm;
    }

    [HttpGet("/diagnostics")]
    public IActionResult Diagnostics()
    {
        return View();
    }
}