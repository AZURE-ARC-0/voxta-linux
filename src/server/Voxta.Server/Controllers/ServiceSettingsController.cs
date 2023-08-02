using System.Text.Json;
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
using Voxta.Abstractions.System;
using Voxta.Services.AzureSpeechService;
using Voxta.Services.NovelAI.Presets;
#if(WINDOWS)
using Voxta.Services.WindowsSpeech;
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
    
    [HttpGet("/settings/azurespeechservice")]
    public async Task<IActionResult> AzureSpeechServiceSettings(CancellationToken cancellationToken)
    {
        var azurespeechservice = await _settingsRepository.GetAsync<AzureSpeechServiceSettings>(cancellationToken) ?? new AzureSpeechServiceSettings
        {
            SubscriptionKey = "",
            Region = "",
        };
        if (!string.IsNullOrEmpty(azurespeechservice.SubscriptionKey)) azurespeechservice.SubscriptionKey = _encryptionProvider.Decrypt(azurespeechservice.SubscriptionKey);  
        return View(azurespeechservice);
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
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/vosk")]
    public async Task<IActionResult> VoskSettings(CancellationToken cancellationToken)
    {
        var vosk = await _settingsRepository.GetAsync<VoskSettings>(cancellationToken) ?? new VoskSettings();
        return View(vosk);
    }
    
    [HttpPost("/settings/vosk")]
    public async Task<IActionResult> PostVoskSettings([FromForm] VoskSettings value)
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
        });
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/elevenlabs")]
    public async Task<IActionResult> ElevenLabsSettings(CancellationToken cancellationToken)
    {
        var elevenlabs = await _settingsRepository.GetAsync<ElevenLabsSettings>(cancellationToken) ?? new ElevenLabsSettings
        {
            ApiKey = "",
        };
        var vm = new ElevenLabsSettingsViewModel
        {
            Enabled = elevenlabs.Enabled,
            Model = elevenlabs.Model,
            ApiKey = !string.IsNullOrEmpty(elevenlabs.ApiKey) ? _encryptionProvider.Decrypt(elevenlabs.ApiKey) : "",
            Parameters = JsonSerializer.Serialize(elevenlabs.Parameters ?? new ElevenLabsParameters()),
            ThinkingSpeech = string.Join('\n', elevenlabs.ThinkingSpeech),
            UseDefaults = elevenlabs.Parameters == null,
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
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/textgenerationwebui")]
    public async Task<IActionResult> TextGenerationWebUISettings(CancellationToken cancellationToken)
    {
        var oobabooga = await _settingsRepository.GetAsync<OobaboogaSettings>(cancellationToken) ?? new OobaboogaSettings
        {
            Uri = "http://127.0.0.1:5000",
        };
        var vm = new OobaboogaSettingsViewModel
        {
            Enabled = oobabooga.Enabled,
            Uri = oobabooga.Uri,
            Parameters = JsonSerializer.Serialize(oobabooga.Parameters ?? new OobaboogaParameters()),
            UseDefaults = oobabooga.Parameters == null,
        };
        return View(vm);
    }
    
    [HttpPost("/settings/textgenerationwebui")]
    public async Task<IActionResult> PostTextGenerationWebUISettings([FromForm] OobaboogaSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("TextGenerationWebUISettings", value);
        }

        await _settingsRepository.SaveAsync(new OobaboogaSettings
        {
            Enabled = value.Enabled,
            Uri = value.Uri.TrimCopyPasteArtefacts(),
            Parameters = value.UseDefaults ? null : JsonSerializer.Deserialize<OobaboogaParameters>(value.Parameters) ?? new OobaboogaParameters(),
        });
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/koboldai")]
    public async Task<IActionResult> KoboldAISettings(CancellationToken cancellationToken)
    {
        var koboldai = await _settingsRepository.GetAsync<KoboldAISettings>(cancellationToken) ?? new KoboldAISettings
        {
            Uri = "http://127.0.0.1:5001",
        };
        var vm = new KoboldAISettingsViewModel
        {
            Enabled = koboldai.Enabled,
            Uri = koboldai.Uri,
            Parameters = JsonSerializer.Serialize(koboldai.Parameters ?? new KoboldAIParameters()),
            UseDefaults = koboldai.Parameters == null,
        };
        return View(vm);
    }
    
    [HttpPost("/settings/koboldai")]
    public async Task<IActionResult> PostKoboldAISettings([FromForm] KoboldAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("KoboldAISettings", value);
        }

        await _settingsRepository.SaveAsync(new KoboldAISettings
        {
            Enabled = value.Enabled,
            Uri = value.Uri.TrimCopyPasteArtefacts(),
            Parameters = value.UseDefaults ? null : JsonSerializer.Deserialize<KoboldAIParameters>(value.Parameters) ?? new KoboldAIParameters(),
        });
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/novelai")]
    public async Task<IActionResult> NovelAISettings(CancellationToken cancellationToken)
    {
        var novelai = await _settingsRepository.GetAsync<NovelAISettings>(cancellationToken) ?? new NovelAISettings
        {
            Token = "",
        };
        var vm = new NovelAISettingsViewModel
        {
            Enabled = novelai.Enabled,
            Model = novelai.Model,
            Token = !string.IsNullOrEmpty(novelai.Token) ? _encryptionProvider.Decrypt(novelai.Token) : "",
            Parameters = JsonSerializer.Serialize(novelai.Parameters ?? NovelAIPresets.DefaultForModel(novelai.Model)),
            ThinkingSpeech = string.Join('\n', novelai.ThinkingSpeech),
            UseDefaults = novelai.Parameters == null,
        };   
        return View(vm);
    }
    
    [HttpPost("/settings/novelai")]
    public async Task<IActionResult> PostNovelAISettings([FromForm] NovelAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("NovelAISettings", value);
        }

        await _settingsRepository.SaveAsync(new NovelAISettings
        {
            Enabled = value.Enabled,
            Token = string.IsNullOrEmpty(value.Token) ? "" : _encryptionProvider.Encrypt(value.Token.TrimCopyPasteArtefacts()),
            Model = value.Model.TrimCopyPasteArtefacts(),
            Parameters = value.UseDefaults ? null : JsonSerializer.Deserialize<NovelAIParameters>(value.Parameters) ?? NovelAIPresets.DefaultForModel(value.Model),
            ThinkingSpeech = value.ThinkingSpeech.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        });
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/openai")]
    public async Task<IActionResult> OpenAISettings(CancellationToken cancellationToken)
    {
        var openai = await _settingsRepository.GetAsync<OpenAISettings>(cancellationToken) ?? new OpenAISettings
        {
            ApiKey = "",
        };
        if (!string.IsNullOrEmpty(openai.ApiKey)) openai.ApiKey = _encryptionProvider.Decrypt(openai.ApiKey);  
        var vm = new OpenAISettingsViewModel
        {
            Enabled = openai.Enabled,
            ApiKey = openai.ApiKey,
            Model = openai.Model,
            Parameters = JsonSerializer.Serialize(openai.Parameters ?? new OpenAIParameters()),
            UseDefaults = openai.Parameters == null,
        };
        return View(vm);
    }
    
    [HttpPost("/settings/openai")]
    public async Task<IActionResult> PostOpenAISettings([FromForm] OpenAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("OpenAISettings", value);
        }

        await _settingsRepository.SaveAsync(new OpenAISettings
        {
            Enabled = value.Enabled,
            ApiKey = string.IsNullOrEmpty(value.ApiKey) ? "" : _encryptionProvider.Encrypt(value.ApiKey.TrimCopyPasteArtefacts()),
            Model = value.Model.TrimCopyPasteArtefacts(),
            Parameters = value.UseDefaults ? null : JsonSerializer.Deserialize<OpenAIParameters>(value.Parameters) ?? new OpenAIParameters(),
        });
        
        return RedirectToAction("Settings", "Settings");
    }
    
    #if(WINDOWS)
    
    [HttpGet("/settings/windowsspeech")]
    public async Task<IActionResult> WindowsSpeechSettings(CancellationToken cancellationToken)
    {
        var windowsspeech = await _settingsRepository.GetAsync<WindowsSpeechSettings>(cancellationToken) ?? new WindowsSpeechSettings();
        var vm = new WindowsSpeechSettingsViewModel
        {
            Enabled = windowsspeech.Enabled
        };
        return View(vm);
    }
    
    [HttpPost("/settings/windowsspeech")]
    public async Task<IActionResult> PostWindowsSpeechSettings([FromForm] WindowsSpeechSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("WindowsSpeechSettings", value);
        }

        await _settingsRepository.SaveAsync(new WindowsSpeechSettings
        {
            Enabled = value.Enabled,
        });
        
        return RedirectToAction("Settings", "Settings");
    }
    
    #endif
}