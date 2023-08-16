#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Repositories;
using Voxta.Common;
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
    private readonly IServicesRepository _servicesRepository;
    private readonly ILocalEncryptionProvider _encryptionProvider;

    public ServiceSettingsController(IServicesRepository servicesRepository, ILocalEncryptionProvider encryptionProvider)
    {
        _servicesRepository = servicesRepository;
        _encryptionProvider = encryptionProvider;
    }
    
    [HttpGet("/settings/mocks/{serviceId:guid}")]
    public async Task<IActionResult> MockSettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<MockSettings>(serviceId, cancellationToken) ?? new ConfiguredService<MockSettings>
        {
            Id = serviceId,
            ServiceName = MockConstants.ServiceName,
            Settings = new MockSettings(),
        };
        return View(settings);
    }
    
    [HttpPost("/settings/mocks/{serviceId:guid}")]
    public async Task<IActionResult> PostMockSettings([FromRoute] Guid serviceId, [FromForm] ConfiguredService<MockSettings> value)
    {
        if (!ModelState.IsValid)
        {
            return View("MockSettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        await _servicesRepository.SaveAsync(new ConfiguredService<MockSettings>
        {
            Id = serviceId,
            ServiceName = MockConstants.ServiceName,
            Label = value.Label,
            Enabled = value.Enabled,
            Settings = value.Settings,
        });
        
        return RedirectToAction("MockSettings", new { serviceId });
    }

    [HttpPost("/settings/mocks/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteMockSettings([FromRoute] Guid serviceId)
    {
        var current = await _servicesRepository.GetAsync<MockSettings>(serviceId);
        if (current != null)
            await _servicesRepository.DeleteAsync(current.Id);

        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/azurespeechservice/{serviceId:guid}")]
    public async Task<IActionResult> AzureSpeechServiceSettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<AzureSpeechServiceSettings>(serviceId, cancellationToken) ?? new ConfiguredService<AzureSpeechServiceSettings>
        {
            Id = default,
            ServiceName = AzureSpeechServiceConstants.ServiceName,
            Settings = new AzureSpeechServiceSettings
            {

                SubscriptionKey = "",
                Region = "",
            },
        };
        settings.Settings.SubscriptionKey = _encryptionProvider.SafeDecrypt(settings.Settings.SubscriptionKey);  
        return View(settings);
    }
    
    [HttpPost("/settings/azurespeechservice/{serviceId:guid}")]
    public async Task<IActionResult> PostAzureSpeechServiceSettings([FromRoute] Guid serviceId, [FromForm] ConfiguredService<AzureSpeechServiceSettings> value)
    {
        if (!ModelState.IsValid)
        {
            return View("AzureSpeechServiceSettings", value);
        }
        
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        await _servicesRepository.SaveAsync(new ConfiguredService<AzureSpeechServiceSettings>
        {
            Id = serviceId,
            ServiceName = AzureSpeechServiceConstants.ServiceName,
            Label = value.Label,
            Enabled = value.Enabled,
            Settings = new AzureSpeechServiceSettings
            {
                Id = serviceId,
                SubscriptionKey = _encryptionProvider.Encrypt(value.Settings.SubscriptionKey.TrimCopyPasteArtefacts()),
                Region = value.Settings.Region.TrimCopyPasteArtefacts(),
                LogFilename = value.Settings.LogFilename?.TrimCopyPasteArtefacts(),
                FilterProfanity = value.Settings.FilterProfanity,
            }
        });
        
        return RedirectToAction("AzureSpeechServiceSettings", new { serviceId });
    }

    [HttpPost("/settings/azurespeechservice/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteAzureSpeechServiceSettings([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);

        return RedirectToAction("Settings", "Settings");
    }

    [HttpGet("/settings/vosk/{serviceId:guid}")]
    public async Task<IActionResult> VoskSettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var vosk = await _servicesRepository.GetAsync<VoskSettings>(serviceId, cancellationToken) ?? new ConfiguredService<VoskSettings>
        {
            Id = serviceId,
            Settings = new VoskSettings(),
            ServiceName = VoskConstants.ServiceName,
        };
        var vm = new VoskSettingsViewModel
        {
            Label = vosk.Label,
            Enabled = vosk.Enabled,
            Model = vosk.Settings.Model,
            ModelHash = vosk.Settings.ModelHash,
            IgnoredWords = string.Join(", ", vosk.Settings.IgnoredWords),
        };
        return View(vm);
    }
    
    [HttpPost("/settings/vosk/{serviceId:guid}")]
    public async Task<IActionResult> PostVoskSettings([FromRoute] Guid serviceId, [FromForm] VoskSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("VoskSettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        await _servicesRepository.SaveAsync(new ConfiguredService<VoskSettings>
        {
            Id = serviceId,
            ServiceName = VoskConstants.ServiceName,
            Label = value.Label,
            Enabled = value.Enabled,
            Settings = new VoskSettings
            {
                Model = value.Model.TrimCopyPasteArtefacts(),
                ModelHash = value.ModelHash?.TrimCopyPasteArtefacts() ?? "",
                IgnoredWords = value.IgnoredWords.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            }
        });
        
        return RedirectToAction("VoskSettings", new { serviceId });
    }
    
    [HttpPost("/settings/vosk/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteVoskSettings([FromRoute] Guid serviceId)
    {
        var current = await _servicesRepository.GetAsync<VoskSettings>(serviceId);
        if (current != null)
            await _servicesRepository.DeleteAsync(current.Id);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/elevenlabs/{serviceId:guid}")]
    public async Task<IActionResult> ElevenLabsSettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<ElevenLabsSettings>(serviceId, cancellationToken) ?? new ConfiguredService<ElevenLabsSettings>
        {
            Id = serviceId,
            ServiceName = ElevenLabsConstants.ServiceName,
            Settings = new ElevenLabsSettings
            {
                ApiKey = "",
            }
        };
        var vm = new ElevenLabsSettingsViewModel
        {
            Enabled = settings.Enabled,
            Label = settings.Label,
            Model = settings.Settings.Model,
            ApiKey = _encryptionProvider.SafeDecrypt(settings.Settings.ApiKey),
            Parameters = JsonSerializer.Serialize(settings.Settings.Parameters ?? new ElevenLabsParameters()),
            ThinkingSpeech = string.Join('\n', settings.Settings.ThinkingSpeech),
            UseDefaults = settings.Settings.Parameters == null,
        };   
        return View(vm);
    }
    
    [HttpPost("/settings/elevenlabs/{serviceId:guid}")]
    public async Task<IActionResult> PostElevenLabsSettings([FromRoute] Guid serviceId, [FromForm] ElevenLabsSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("ElevenLabsSettings", value);
        }
        
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        await _servicesRepository.SaveAsync(new ConfiguredService<ElevenLabsSettings>
        {
            Id = serviceId,
            ServiceName = ElevenLabsConstants.ServiceName,
            Label = value.Label,
            Enabled = value.Enabled,
            Settings = new ElevenLabsSettings
            {
                ApiKey = string.IsNullOrEmpty(value.ApiKey) ? "" : _encryptionProvider.Encrypt(value.ApiKey.TrimCopyPasteArtefacts()),
                Model = value.Model.TrimCopyPasteArtefacts(),
                Parameters = value.UseDefaults ? null : JsonSerializer.Deserialize<ElevenLabsParameters>(value.Parameters) ?? new ElevenLabsParameters(),
                ThinkingSpeech = value.ThinkingSpeech.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            },
        });
        
        return RedirectToAction("ElevenLabsSettings", new { serviceId });
    }

    [HttpPost("/settings/elevenlabs/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteElevenLabsSettings([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }

    [HttpGet("/settings/textgenerationwebui/{serviceId:guid}")]
    public async Task<IActionResult> TextGenerationWebUISettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<OobaboogaSettings>(serviceId, cancellationToken) ?? new ConfiguredService<OobaboogaSettings>
        {
            Id = serviceId,
            ServiceName = OobaboogaConstants.ServiceName,
            Settings = new OobaboogaSettings
            {
                Uri = "http://127.0.0.1:5000",
            }
        };
        var vm = new OobaboogaSettingsViewModel(settings);
        return View(vm);
    }
    
    [HttpPost("/settings/textgenerationwebui/{serviceId:guid}")]
    public async Task<IActionResult> PostTextGenerationWebUISettings([FromRoute] Guid serviceId, [FromForm] OobaboogaSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("TextGenerationWebUISettings", value);
        }
        
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        
        return RedirectToAction("TextGenerationWebUISettings", new { serviceId });
    }
    
    [HttpPost("/settings/textgenerationwebui/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteTextGenerationWebUISettings([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/koboldai/{serviceId:guid}")]
    public async Task<IActionResult> KoboldAISettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<KoboldAISettings>(serviceId, cancellationToken) ?? new ConfiguredService<KoboldAISettings>
        {
            Id = serviceId,
            ServiceName = KoboldAIConstants.ServiceName,
            Settings = new KoboldAISettings
            {
                Uri = "http://127.0.0.1:5001",
            },
        };
        var vm = new KoboldAISettingsViewModel(settings);
        return View(vm);
    }
    
    [HttpPost("/settings/koboldai/{serviceId:guid}")]
    public async Task<IActionResult> PostKoboldAISettings([FromRoute] Guid serviceId, [FromForm] KoboldAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("KoboldAISettings", value);
        }
        
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        
        return RedirectToAction("KoboldAISettings", new { serviceId });
    }
    
    [HttpPost("/settings/koboldai/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteKoboldAISettings([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }

    [HttpGet("/settings/textgenerationinference/{serviceId:guid}")]
    public async Task<IActionResult> TextGenerationInferenceSettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<TextGenerationInferenceSettings>(serviceId, cancellationToken) ?? new ConfiguredService<TextGenerationInferenceSettings>
        {
            Id = serviceId,
            ServiceName = TextGenerationInferenceConstants.ServiceName,
            Settings = new TextGenerationInferenceSettings
            {
                Uri = "http://127.0.0.1:8080",
            },
        };
        var vm = new TextGenerationInferenceSettingsViewModel(settings);
        return View(vm);
    }
    
    [HttpPost("/settings/textgenerationinference/{serviceId:guid}")]
    public async Task<IActionResult> PostTextGenerationInferenceSettings([FromRoute] Guid serviceId, [FromForm] TextGenerationInferenceSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("TextGenerationInferenceSettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        
        return RedirectToAction("TextGenerationInferenceSettings", new { serviceId });
    }
    
    [HttpPost("/settings/textgenerationinference/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteTextGenerationInferenceSettings([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/novelai/{serviceId:guid}")]
    public async Task<IActionResult> NovelAISettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<NovelAISettings>(serviceId, cancellationToken) ?? new ConfiguredService<NovelAISettings>
        {
            Id = serviceId,
            ServiceName = NovelAIConstants.ServiceName,
            Settings = new NovelAISettings
            {
                Token = "",
            }
        };
        var vm = new NovelAISettingsViewModel(settings, _encryptionProvider);
        return View(vm);
    }
    
    [HttpPost("/settings/novelai/{serviceId:guid}")]
    public async Task<IActionResult> PostNovelAISettings([FromRoute] Guid serviceId, [FromForm] NovelAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("NovelAISettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId, _encryptionProvider);
        await _servicesRepository.SaveAsync(settings);
        
        return RedirectToAction("NovelAISettings", new { serviceId });
    }

    [HttpPost("/settings/novelai/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteNovelAISettings([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);

        return RedirectToAction("Settings", "Settings");
    }

    [HttpGet("/settings/openai/{serviceId:guid}")]
    public async Task<IActionResult> OpenAISettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<OpenAISettings>(serviceId, cancellationToken) ?? new ConfiguredService<OpenAISettings>
        {
            Id = serviceId,
            ServiceName = OpenAIConstants.ServiceName,
            Settings = new OpenAISettings
            {
                ApiKey = "",
            }
        };

        var vm = new OpenAISettingsViewModel(settings, _encryptionProvider);
        return View(vm);
    }
    
    [HttpPost("/settings/openai/{serviceId:guid}")]
    public async Task<IActionResult> PostOpenAISettings([FromRoute] Guid serviceId, [FromForm] OpenAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("OpenAISettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId, _encryptionProvider);
        await _servicesRepository.SaveAsync(settings);
        
        return RedirectToAction("OpenAISettings", new { serviceId });
    }
    
    [HttpPost("/settings/openai/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteOpenAISettings([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/windowsspeech/{serviceId:guid}")]
    public async Task<IActionResult> WindowsSpeechSettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
    
        #if(WINDOWS)
        var settings = await _servicesRepository.GetAsync<WindowsSpeechSettings>(serviceId, cancellationToken) ?? new ConfiguredService<WindowsSpeechSettings>
        {
            Id = serviceId,
            ServiceName = WindowsSpeechConstants.ServiceName,
            Settings = new WindowsSpeechSettings()
        };
        var vm = new WindowsSpeechSettingsViewModel
        {
            Label = settings.Label,
            Enabled = settings.Enabled,
            MinimumConfidence = settings.Settings.MinimumConfidence,
        };
        return View(vm);
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/windowsspeech/{serviceId:guid}")]
    public async Task<IActionResult> PostWindowsSpeechSettings([FromRoute] Guid serviceId, [FromForm] WindowsSpeechSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("WindowsSpeechSettings", value);
        }

        #if(WINDOWS)
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        await _servicesRepository.SaveAsync(new ConfiguredService<WindowsSpeechSettings>
        {
            Id = serviceId,
            ServiceName = WindowsSpeechConstants.ServiceName,
            Label = value.Label,
            Enabled = value.Enabled,
            Settings = new WindowsSpeechSettings
            {
                MinimumConfidence = value.MinimumConfidence,
            }
        });
        
        return RedirectToAction("WindowsSpeechSettings", new { serviceId });
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/windowsspeech/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteWindowsSpeechSettings([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/ffmpeg/{serviceId:guid}")]
    public async Task<IActionResult> FFmpegSettings([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        #if(!WINDOWS)
        var ffmpeg = await _servicesRepository.GetAsync<FFmpegSettings>(serviceId, cancellationToken) ?? new ConfiguredService<FFmpegSettings>
        {
            Id = serviceId,
            ServiceName = FFmpegConstants.ServiceName,
            Settings = new FFmpegSettings(),
        };
        var vm = new FFmpegSettingsViewModel
        {
            Enabled = ffmpeg.Enabled,
            Label = "",
        };
        return View(vm);
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/ffmpeg/{serviceId:guid}")]
    public async Task<IActionResult> PostFFmpegSettings([FromRoute] Guid serviceId, [FromForm] FFmpegSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("FFmpegSettings", value);
        }
        #if(!WINDOWS)
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        await _servicesRepository.SaveAsync(new ConfiguredService<FFmpegSettings>
        {
            Id = serviceId,
            ServiceName = FFMpegConstants.ServiceName,
            Label = value.Enabled,
            Enabled = value.Enabled,
            Settings = new FFmpegSettings
            {
            }
        });
        
        return RedirectToAction("FFmpegSettings", new { serviceId });
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/ffmpeg/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteFFmpegSettings([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }
}