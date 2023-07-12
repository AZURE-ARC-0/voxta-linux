using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using ChatMate.Server.ViewModels;
using ChatMate.Services.KoboldAI;
using ChatMate.Services.ElevenLabs;
using ChatMate.Services.NovelAI;
using ChatMate.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers.Pages;

[Controller]
public class BotsController : Controller
{
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
                Preamble = "{{char}} is a virtual companion for {{user}}.",
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
            if (isNew) bot.Id = Crypto.CreateCryptographicallySecureGuid().ToString();
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
}