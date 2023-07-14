using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Common;
using ChatMate.Server.ViewModels;
using ChatMate.Services.KoboldAI;
using ChatMate.Services.ElevenLabs;
using ChatMate.Services.NovelAI;
using ChatMate.Services.Oobabooga;
using ChatMate.Services.OpenAI;
using ChatMate.Services.Vosk;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers.Pages;

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
            Profile = profile ?? new ProfileSettings
            {
                Name = "User",
                Description = "",
                EnableSpeechRecognition = true,
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
        await profileRepository.SaveProfileAsync(new ProfileSettings
        {
            Name = model.Profile.Name.Trim(),
            Description = model.Profile.Description?.Trim(),
            EnableSpeechRecognition = model.Profile.EnableSpeechRecognition,
            PauseSpeechRecognitionDuringPlayback = model.Profile.PauseSpeechRecognitionDuringPlayback,
            Services = model.Profile.Services
        });
        
        return RedirectToAction("Settings");
    }
}