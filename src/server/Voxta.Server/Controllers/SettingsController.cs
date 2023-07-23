using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Common;
using Voxta.Server.ViewModels;
using Voxta.Services.KoboldAI;
using Voxta.Services.ElevenLabs;
using Voxta.Services.NovelAI;
using Voxta.Services.Oobabooga;
using Voxta.Services.OpenAI;
using Voxta.Services.Vosk;
using Microsoft.AspNetCore.Mvc;
using Voxta.Services.AzureSpeechService;

namespace Voxta.Server.Controllers;

[Controller]
public class SettingsController : Controller
{
    [HttpGet("/settings")]
    public async Task<IActionResult> Settings(
        [FromServices] ISettingsRepository settingsRepository,
        [FromServices] IProfileRepository profileRepository,
        CancellationToken cancellationToken)
    {
        var openai = await settingsRepository.GetAsync<OpenAISettings>(cancellationToken);
        var novelai = await settingsRepository.GetAsync<NovelAISettings>(cancellationToken);
        var koboldai = await settingsRepository.GetAsync<KoboldAISettings>(cancellationToken);
        var oobabooga = await settingsRepository.GetAsync<OobaboogaSettings>(cancellationToken);
        var elevenlabs = await settingsRepository.GetAsync<ElevenLabsSettings>(cancellationToken);
        var azurespeechservice = await settingsRepository.GetAsync<AzureSpeechServiceSettings>(cancellationToken);
        var profile = await profileRepository.GetProfileAsync(cancellationToken);

        var vm = new SettingsViewModel
        {
            OpenAI = new OpenAISettings
            {
                ApiKey = string.IsNullOrEmpty(openai?.ApiKey) ? null : Crypto.DecryptString(openai.ApiKey),
                Model = openai?.Model ?? OpenAISettings.DefaultModel,
            },
            NovelAI = new NovelAISettings
            {
                Token = string.IsNullOrEmpty(novelai?.Token) ? null : Crypto.DecryptString(novelai.Token),
                Model = novelai?.Model ?? NovelAISettings.DefaultModel,
            },
            KoboldAI = new KoboldAISettings
            {
                Uri = koboldai?.Uri,
            },
            Oobabooga = new OobaboogaSettings
            {
                Uri = oobabooga?.Uri,
            },
            ElevenLabs = new ElevenLabsSettings
            {
              ApiKey  = string.IsNullOrEmpty(elevenlabs?.ApiKey) ? null : Crypto.DecryptString(elevenlabs.ApiKey),
            },
            AzureSpeechService = new AzureSpeechServiceSettings
            {
                SubscriptionKey = string.IsNullOrEmpty(azurespeechservice?.SubscriptionKey) ? null : Crypto.DecryptString(azurespeechservice.SubscriptionKey),
                Region = azurespeechservice?.Region ?? null,
            },
            Profile = profile ?? new ProfileSettings
            {
                Name = "User",
                Description = "",
                PauseSpeechRecognitionDuringPlayback = true,
                Services = new ProfileSettings.ProfileServicesMap
                {
                    ActionInference =  new ServiceMap
                    {
                        Service = OpenAIConstants.ServiceName,
                    },
                    SpeechToText = new SpeechToTextServiceMap
                    {
                        Service = VoskConstants.ServiceName,
                        Model = "vosk-model-small-en-us-0.15",
                        Hash = "30f26242c4eb449f948e42cb302dd7a686cb29a3423a8367f99ff41780942498",
                    }
                }
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
        
        await settingsRepository.SaveAsync(new OpenAISettings
        {
            ApiKey = string.IsNullOrEmpty(model.OpenAI.ApiKey) ? null : Crypto.EncryptString(model.OpenAI.ApiKey.Trim('"', ' ')),
            Model = model.OpenAI.Model.Trim(),
        });
        await settingsRepository.SaveAsync(new NovelAISettings
        {
            Token = string.IsNullOrEmpty(model.NovelAI.Token) ? null : Crypto.EncryptString(model.NovelAI.Token.Trim('"', ' ')),
            Model = model.NovelAI.Model.Trim(),
        });
        await settingsRepository.SaveAsync(new KoboldAISettings
        {
            Uri = model.KoboldAI.Uri,
        });
        await settingsRepository.SaveAsync(new OobaboogaSettings
        {
            Uri = model.Oobabooga.Uri,
        });
        await settingsRepository.SaveAsync(new ElevenLabsSettings
        {
            ApiKey = string.IsNullOrEmpty(model.ElevenLabs.ApiKey) ? null : Crypto.EncryptString(model.ElevenLabs.ApiKey.Trim('"', ' ')),
        });
        await settingsRepository.SaveAsync(new AzureSpeechServiceSettings
        {
            SubscriptionKey = string.IsNullOrEmpty(model.AzureSpeechService.SubscriptionKey) ? null : Crypto.EncryptString(model.AzureSpeechService.SubscriptionKey.Trim('"', ' ')),
            Region = model.AzureSpeechService.Region?.Trim() ?? "",
        });
        await profileRepository.SaveProfileAsync(new ProfileSettings
        {
            Name = model.Profile.Name.Trim(),
            Description = model.Profile.Description?.Trim(),
            PauseSpeechRecognitionDuringPlayback = model.Profile.PauseSpeechRecognitionDuringPlayback,
            Services = model.Profile.Services
        });
        
        return RedirectToAction("Settings");
    }
}