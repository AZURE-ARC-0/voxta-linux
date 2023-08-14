#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Repositories;
using Voxta.Server.ViewModels.ServiceSettings;
using Voxta.Services.KoboldAI;
using Voxta.Services.ElevenLabs;
using Voxta.Services.NovelAI;
using Voxta.Services.Oobabooga;
using Voxta.Services.OpenAI;
using Voxta.Services.Vosk;
using Voxta.Services.AzureSpeechService;
using Voxta.Services.Mocks;
using Voxta.Services.TextGenerationInference;
#if(WINDOWS)
using Voxta.Services.WindowsSpeech;
#endif
#if(!WINDOWS)
using Voxta.Services.FFmpeg;
#endif

namespace Voxta.Server.Controllers;

[Controller]
public class ServiceSettingsController : Controller
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILocalEncryptionProvider _encryptionProvider;

    public ServiceSettingsController(ISettingsRepository settingsRepository, ILocalEncryptionProvider encryptionProvider)
    {
        _settingsRepository = settingsRepository;
        _encryptionProvider = encryptionProvider;
    }
    
    [HttpGet("/settings/mocks")]
    public async Task<IActionResult> MockSettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<MockSettings>(cancellationToken) ?? new MockSettings();
        return View(settings);
    }
    
    [HttpPost("/settings/mocks")]
    public async Task<IActionResult> PostMockSettings([FromForm] MockSettings value)
    {
        if (!ModelState.IsValid)
        {
            return View("MockSettings", value);
        }

        await _settingsRepository.SaveAsync(new MockSettings
        {
            Enabled = value.Enabled,
        });
        
        return RedirectToAction("MockSettings");
    }

    [HttpPost("/settings/mocks/reset")]
    public async Task<IActionResult> ResetMockSettings()
    {
        var current = await _settingsRepository.GetAsync<MockSettings>();
        if (current != null)
            await _settingsRepository.DeleteAsync(current);

        return RedirectToAction("MockSettings");
    }
    
    [HttpGet("/settings/azurespeechservice")]
    public async Task<IActionResult> AzureSpeechServiceSettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<AzureSpeechServiceSettings>(cancellationToken) ?? new AzureSpeechServiceSettings
        {
            SubscriptionKey = "",
            Region = "",
        };
        settings.SubscriptionKey = _encryptionProvider.SafeDecrypt(settings.SubscriptionKey);  
        return View(settings);
    }
    
    [HttpPost("/settings/azurespeechservice")]
    public async Task<IActionResult> PostAzureSpeechServiceSettings([FromForm] AzureSpeechServiceSettings value)
    {
        if (!ModelState.IsValid)
        {
            return View("AzureSpeechServiceSettings", value);
        }

        await _settingsRepository.SaveAsync(new AzureSpeechServiceSettings
        {
            Enabled = value.Enabled,
            SubscriptionKey = string.IsNullOrEmpty(value.SubscriptionKey) ? "" : _encryptionProvider.Encrypt(value.SubscriptionKey.TrimCopyPasteArtefacts()),
            Region = value.Region.TrimCopyPasteArtefacts(),
            LogFilename = value.LogFilename?.TrimCopyPasteArtefacts(),
            FilterProfanity = value.FilterProfanity,
        });
        
        return RedirectToAction("AzureSpeechServiceSettings");
    }

    [HttpPost("/settings/azurespeechservice/reset")]
    public async Task<IActionResult> ResetAzureSpeechServiceSettings()
    {
        var current = await _settingsRepository.GetAsync<AzureSpeechServiceSettings>();
        if (!string.IsNullOrEmpty(current?.SubscriptionKey))
            await _settingsRepository.SaveAsync(new AzureSpeechServiceSettings
            {
                SubscriptionKey = current.SubscriptionKey,
                Region = current.Region,
            });
        else if (current != null)
            await _settingsRepository.DeleteAsync(current);

        return RedirectToAction("AzureSpeechServiceSettings");
    }

    [HttpGet("/settings/vosk")]
    public async Task<IActionResult> VoskSettings(CancellationToken cancellationToken)
    {
        var vosk = await _settingsRepository.GetAsync<VoskSettings>(cancellationToken) ?? new VoskSettings();
        var vm = new VoskSettingsViewModel
        {
            Model = vosk.Model,
            ModelHash = vosk.ModelHash,
            IgnoredWords = string.Join(", ", vosk.IgnoredWords),
        };
        return View(vm);
    }
    
    [HttpPost("/settings/vosk")]
    public async Task<IActionResult> PostVoskSettings([FromForm] VoskSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("VoskSettings", value);
        }

        await _settingsRepository.SaveAsync(new VoskSettings
        {
            Enabled = value.Enabled,
            Model = value.Model.TrimCopyPasteArtefacts(),
            ModelHash = value.ModelHash?.TrimCopyPasteArtefacts() ?? "",
            IgnoredWords = value.IgnoredWords.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
        });
        
        return RedirectToAction("VoskSettings");
    }
    
    [HttpPost("/settings/vosk/reset")]
    public async Task<IActionResult> ResetVoskSettings()
    {
        var current = await _settingsRepository.GetAsync<VoskSettings>();
        if (current != null)
            await _settingsRepository.DeleteAsync(current);
        
        return RedirectToAction("VoskSettings");
    }
    
    [HttpGet("/settings/elevenlabs")]
    public async Task<IActionResult> ElevenLabsSettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<ElevenLabsSettings>(cancellationToken) ?? new ElevenLabsSettings
        {
            ApiKey = "",
        };
        var vm = new ElevenLabsSettingsViewModel
        {
            Enabled = settings.Enabled,
            Model = settings.Model,
            ApiKey = _encryptionProvider.SafeDecrypt(settings.ApiKey),
            Parameters = JsonSerializer.Serialize(settings.Parameters ?? new ElevenLabsParameters()),
            ThinkingSpeech = string.Join('\n', settings.ThinkingSpeech),
            UseDefaults = settings.Parameters == null,
        };   
        return View(vm);
    }
    
    [HttpPost("/settings/elevenlabs")]
    public async Task<IActionResult> PostElevenLabsSettings([FromForm] ElevenLabsSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("ElevenLabsSettings", value);
        }

        await _settingsRepository.SaveAsync(new ElevenLabsSettings
        {
            Enabled = value.Enabled,
            ApiKey = string.IsNullOrEmpty(value.ApiKey) ? "" : _encryptionProvider.Encrypt(value.ApiKey.TrimCopyPasteArtefacts()),
            Model = value.Model.TrimCopyPasteArtefacts(),
            Parameters = value.UseDefaults ? null : JsonSerializer.Deserialize<ElevenLabsParameters>(value.Parameters) ?? new ElevenLabsParameters(),
            ThinkingSpeech = value.ThinkingSpeech.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        });
        
        return RedirectToAction("ElevenLabsSettings");
    }

    [HttpPost("/settings/elevenlabs/reset")]
    public async Task<IActionResult> ResetElevenLabsSettings()
    {
        var current = await _settingsRepository.GetAsync<ElevenLabsSettings>();
        if (!string.IsNullOrEmpty(current?.ApiKey))
            await _settingsRepository.SaveAsync(new ElevenLabsSettings
            {
                ApiKey = current.ApiKey,
            });
        else if (current != null)
            await _settingsRepository.DeleteAsync(current);

        return RedirectToAction("ElevenLabsSettings");
    }

    [HttpGet("/settings/textgenerationwebui")]
    public async Task<IActionResult> TextGenerationWebUISettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<OobaboogaSettings>(cancellationToken) ?? new OobaboogaSettings
        {
            Uri = "http://127.0.0.1:5000",
        };
        var vm = new OobaboogaSettingsViewModel(settings);
        return View(vm);
    }
    
    [HttpPost("/settings/textgenerationwebui")]
    public async Task<IActionResult> PostTextGenerationWebUISettings([FromForm] OobaboogaSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("TextGenerationWebUISettings", value);
        }

        var settings = value.ToSettings();
        await _settingsRepository.SaveAsync(settings);
        
        return RedirectToAction("TextGenerationWebUISettings");
    }
    
    [HttpPost("/settings/textgenerationwebui/reset")]
    public async Task<IActionResult> ResetTextGenerationWebUISettings()
    {
        var current = await _settingsRepository.GetAsync<OobaboogaSettings>();
        if (!string.IsNullOrEmpty(current?.Uri))
            await _settingsRepository.SaveAsync(new OobaboogaSettings
            {
                Uri = current.Uri,
            });
        else if (current != null)
            await _settingsRepository.DeleteAsync(current);
        
        return RedirectToAction("TextGenerationWebUISettings");
    }
    
    [HttpGet("/settings/koboldai")]
    public async Task<IActionResult> KoboldAISettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<KoboldAISettings>(cancellationToken) ?? new KoboldAISettings
        {
            Uri = "http://127.0.0.1:5001",
        };
        var vm = new KoboldAISettingsViewModel(settings);
        return View(vm);
    }
    
    [HttpPost("/settings/koboldai")]
    public async Task<IActionResult> PostKoboldAISettings([FromForm] KoboldAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("KoboldAISettings", value);
        }

        var settings = value.ToSettings();
        await _settingsRepository.SaveAsync(settings);
        
        return RedirectToAction("KoboldAISettings");
    }
    
    [HttpPost("/settings/koboldai/reset")]
    public async Task<IActionResult> ResetKoboldAISettings()
    {
        var current = await _settingsRepository.GetAsync<KoboldAISettings>();
        if (!string.IsNullOrEmpty(current?.Uri))
            await _settingsRepository.SaveAsync(new KoboldAISettings
            {
                Uri = current.Uri,
            });
        else if (current != null)
            await _settingsRepository.DeleteAsync(current);
        
        return RedirectToAction("KoboldAISettings");
    }

    [HttpGet("/settings/textgenerationinference")]
    public async Task<IActionResult> TextGenerationInferenceSettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<TextGenerationInferenceSettings>(cancellationToken) ?? new TextGenerationInferenceSettings
        {
            Uri = "http://127.0.0.1:8080",
        };
        var vm = new TextGenerationInferenceSettingsViewModel(settings);
        return View(vm);
    }
    
    [HttpPost("/settings/textgenerationinference")]
    public async Task<IActionResult> PostTextGenerationInferenceSettings([FromForm] TextGenerationInferenceSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("TextGenerationInferenceSettings", value);
        }

        var settings = value.ToSettings();
        await _settingsRepository.SaveAsync(settings);
        
        return RedirectToAction("TextGenerationInferenceSettings");
    }
    
    [HttpPost("/settings/textgenerationinference/reset")]
    public async Task<IActionResult> ResetTextGenerationInferenceSettings()
    {
        var current = await _settingsRepository.GetAsync<TextGenerationInferenceSettings>();
        if (!string.IsNullOrEmpty(current?.Uri))
            await _settingsRepository.SaveAsync(new TextGenerationInferenceSettings
            {
                Uri = current.Uri,
            });
        else if (current != null)
            await _settingsRepository.DeleteAsync(current);
        
        return RedirectToAction("TextGenerationInferenceSettings");
    }
    
    [HttpGet("/settings/novelai")]
    public async Task<IActionResult> NovelAISettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<NovelAISettings>(cancellationToken) ?? new NovelAISettings
        {
            Token = "",
        };
        var vm = new NovelAISettingsViewModel(settings, _encryptionProvider);
        return View(vm);
    }
    
    [HttpPost("/settings/novelai")]
    public async Task<IActionResult> PostNovelAISettings([FromForm] NovelAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("NovelAISettings", value);
        }

        var settings = value.ToSettings(_encryptionProvider);
        await _settingsRepository.SaveAsync(settings);
        
        return RedirectToAction("NovelAISettings");
    }

    [HttpPost("/settings/novelai/reset")]
    public async Task<IActionResult> ResetNovelAISettings()
    {
        var current = await _settingsRepository.GetAsync<NovelAISettings>();
        if (!string.IsNullOrEmpty(current?.Token))
            await _settingsRepository.SaveAsync(new NovelAISettings
            {
                Token = current.Token,
            });
        else if (current != null)
            await _settingsRepository.DeleteAsync(current);

        return RedirectToAction("NovelAISettings");
    }

    [HttpGet("/settings/openai")]
    public async Task<IActionResult> OpenAISettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<OpenAISettings>(cancellationToken) ?? new OpenAISettings
        {
            ApiKey = "",
        };

        var vm = new OpenAISettingsViewModel(settings, _encryptionProvider);
        return View(vm);
    }
    
    [HttpPost("/settings/openai")]
    public async Task<IActionResult> PostOpenAISettings([FromForm] OpenAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("OpenAISettings", value);
        }

        var settings = value.ToSettings(_encryptionProvider);
        await _settingsRepository.SaveAsync(settings);
        
        return RedirectToAction("OpenAISettings");
    }
    
    [HttpPost("/settings/openai/reset")]
    public async Task<IActionResult> ResetOpenAISettings()
    {
        var current = await _settingsRepository.GetAsync<OpenAISettings>();
        if (!string.IsNullOrEmpty(current?.ApiKey))
            await _settingsRepository.SaveAsync(new OpenAISettings
            {
                ApiKey = current.ApiKey,
            });
        else if (current != null)
            await _settingsRepository.DeleteAsync(current);
        
        return RedirectToAction("OpenAISettings");
    }
    
    [HttpGet("/settings/windowsspeech")]
    public async Task<IActionResult> WindowsSpeechSettings(CancellationToken cancellationToken)
    {
    
        #if(WINDOWS)
        var settings = await _settingsRepository.GetAsync<WindowsSpeechSettings>(cancellationToken) ?? new WindowsSpeechSettings();
        var vm = new WindowsSpeechSettingsViewModel
        {
            Enabled = settings.Enabled,
            MinimumConfidence = settings.MinimumConfidence,
        };
        return View(vm);
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/windowsspeech")]
    public async Task<IActionResult> PostWindowsSpeechSettings([FromForm] WindowsSpeechSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("WindowsSpeechSettings", value);
        }

        #if(WINDOWS)
        await _settingsRepository.SaveAsync(new WindowsSpeechSettings
        {
            Enabled = value.Enabled,
            MinimumConfidence = value.MinimumConfidence,
        });
        
        return RedirectToAction("WindowsSpeechSettings");
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/windowsspeech/reset")]
    public async Task<IActionResult> ResetWindowsSpeechSettings()
    {
        #if(WINDOWS)
        var current = await _settingsRepository.GetAsync<WindowsSpeechSettings>();
        if (current != null)
            await _settingsRepository.DeleteAsync(current);
        return RedirectToAction("WindowsSpeechSettings");
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpGet("/settings/ffmpeg")]
    public async Task<IActionResult> FFmpegSettings(CancellationToken cancellationToken)
    {
        #if(!WINDOWS)
        var ffmpeg = await _settingsRepository.GetAsync<FFmpegSettings>(cancellationToken) ?? new FFmpegSettings();
        var vm = new FFmpegSettingsViewModel
        {
            Enabled = ffmpeg.Enabled
        };
        return View(vm);
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/ffmpeg")]
    public async Task<IActionResult> PostFFmpegSettings([FromForm] FFmpegSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("FFmpegSettings", value);
        }
        #if(!WINDOWS)
        await _settingsRepository.SaveAsync(new FFmpegSettings
        {
            Enabled = value.Enabled,
        });
        
        return RedirectToAction("FFmpegSettings");
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/ffmpeg/reset")]
    public async Task<IActionResult> ResetFFmpegSettings()
    {
        #if(!WINDOWS)
        var current = await _settingsRepository.GetAsync<FFmpegSettings>();
        if (current != null)
            await _settingsRepository.DeleteAsync(current);
        
        return RedirectToAction("FFmpegSettings");
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
}