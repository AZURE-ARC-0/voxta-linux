using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Common;
using ChatMate.Server.ViewModels;
using ChatMate.Services.KoboldAI;
using ChatMate.Services.ElevenLabs;
using ChatMate.Services.NovelAI;
using ChatMate.Services.OpenAI;
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
        var elevenlabs = await settingsRepository.GetAsync<ElevenLabsSettings>(cancellationToken);
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
        
        await settingsRepository.SaveAsync(new OpenAISettings
        {
            ApiKey = string.IsNullOrEmpty(model.OpenAI.ApiKey) ? "" : Crypto.EncryptString(model.OpenAI.ApiKey.Trim('"', ' ')),
            Model = model.OpenAI.Model.Trim(),
        });
        await settingsRepository.SaveAsync(new NovelAISettings
        {
            Token = string.IsNullOrEmpty(model.NovelAI.Token) ? "" : Crypto.EncryptString(model.NovelAI.Token.Trim('"', ' ')),
            Model = model.NovelAI.Model.Trim(),
        });
        await settingsRepository.SaveAsync(new KoboldAISettings
        {
            Uri = model.KoboldAI.Uri,
        });
        await settingsRepository.SaveAsync(new ElevenLabsSettings
        {
            ApiKey = string.IsNullOrEmpty(model.ElevenLabs.ApiKey) ? "" : Crypto.EncryptString(model.ElevenLabs.ApiKey.Trim('"', ' ')),
        });
        await profileRepository.SaveProfileAsync(new ProfileSettings
        {
            Name = model.Profile.Name.Trim(),
            Description = model.Profile.Description?.Trim(),
            EnableSpeechRecognition = model.Profile.EnableSpeechRecognition,
            PauseSpeechRecognitionDuringPlayback = model.Profile.PauseSpeechRecognitionDuringPlayback,
            AnimationSelectionService = model.Profile.AnimationSelectionService,
        });
        
        return RedirectToAction("Chat", "Chat");
    }
}