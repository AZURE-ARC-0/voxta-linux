#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
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
    private readonly IProfileRepository _profileRepository;
    private readonly IServiceDefinitionsRegistry _serviceRegistry;
    private readonly IServicesRepository _servicesRepository;
    private readonly ILocalEncryptionProvider _encryptionProvider;

    public ServiceSettingsController(IServicesRepository servicesRepository, ILocalEncryptionProvider encryptionProvider, IProfileRepository profileRepository, IServiceDefinitionsRegistry serviceRegistry)
    {
        _servicesRepository = servicesRepository;
        _encryptionProvider = encryptionProvider;
        _profileRepository = profileRepository;
        _serviceRegistry = serviceRegistry;
    }
    
    [HttpGet("/settings/mocks/{serviceId:guid}")]
    public async Task<IActionResult> MockSettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<MockSettings>(serviceId, cancellationToken) ?? new ConfiguredService<MockSettings>
        {
            Id = serviceId,
            ServiceName = MockConstants.ServiceName,
            Settings = new MockSettings(),
        };
        var vm = new MockSettingsViewModel(settings);
        return View(vm);
    }
    
    [HttpPost("/settings/mocks/{serviceId:guid}")]
    public async Task<IActionResult> PostMockSettingsAsync([FromRoute] Guid serviceId, [FromForm] MockSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("MockSettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("MockSettings", new { serviceId });
    }

    [HttpPost("/settings/mocks/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteMockSettingsAsync([FromRoute] Guid serviceId)
    {
        var current = await _servicesRepository.GetAsync<MockSettings>(serviceId);
        if (current != null)
            await _servicesRepository.DeleteAsync(current.Id);

        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/azurespeechservice/{serviceId:guid}")]
    public async Task<IActionResult> AzureSpeechServiceSettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
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
        var vm = new AzureSpeechServiceSettingsViewModel(settings, _encryptionProvider);
        return View(vm);
    }
    
    [HttpPost("/settings/azurespeechservice/{serviceId:guid}")]
    public async Task<IActionResult> PostAzureSpeechServiceSettingsAsync([FromRoute] Guid serviceId, [FromForm] AzureSpeechServiceSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("AzureSpeechServiceSettings", value);
        }
        
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId, _encryptionProvider);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("AzureSpeechServiceSettings", new { serviceId });
    }

    [HttpPost("/settings/azurespeechservice/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteAzureSpeechServiceSettingsAsync([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);

        return RedirectToAction("Settings", "Settings");
    }

    [HttpGet("/settings/vosk/{serviceId:guid}")]
    public async Task<IActionResult> VoskSettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var settings = await _servicesRepository.GetAsync<VoskSettings>(serviceId, cancellationToken) ?? new ConfiguredService<VoskSettings>
        {
            Id = serviceId,
            Settings = new VoskSettings(),
            ServiceName = VoskConstants.ServiceName,
        };
        var vm = new VoskSettingsViewModel(settings);
        return View(vm);
    }
    
    [HttpPost("/settings/vosk/{serviceId:guid}")]
    public async Task<IActionResult> PostVoskSettingsAsync([FromRoute] Guid serviceId, [FromForm] VoskSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("VoskSettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("VoskSettings", new { serviceId });
    }
    
    [HttpPost("/settings/vosk/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteVoskSettingsAsync([FromRoute] Guid serviceId)
    {
        var current = await _servicesRepository.GetAsync<VoskSettings>(serviceId);
        if (current != null)
            await _servicesRepository.DeleteAsync(current.Id);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/elevenlabs/{serviceId:guid}")]
    public async Task<IActionResult> ElevenLabsSettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
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
        var vm = new ElevenLabsSettingsViewModel(settings, _encryptionProvider); 
        return View(vm);
    }
    
    [HttpPost("/settings/elevenlabs/{serviceId:guid}")]
    public async Task<IActionResult> PostElevenLabsSettingsAsync([FromRoute] Guid serviceId, [FromForm] ElevenLabsSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("ElevenLabsSettings", value);
        }
        
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId, _encryptionProvider);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("ElevenLabsSettings", new { serviceId });
    }

    [HttpPost("/settings/elevenlabs/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteElevenLabsSettingsAsync([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }

    [HttpGet("/settings/textgenerationwebui/{serviceId:guid}")]
    public async Task<IActionResult> TextGenerationWebUISettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
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
    public async Task<IActionResult> PostTextGenerationWebUISettingsAsync([FromRoute] Guid serviceId, [FromForm] OobaboogaSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("TextGenerationWebUISettings", value);
        }
        
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("TextGenerationWebUISettings", new { serviceId });
    }
    
    [HttpPost("/settings/textgenerationwebui/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteTextGenerationWebUISettingsAsync([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/koboldai/{serviceId:guid}")]
    public async Task<IActionResult> KoboldAISettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
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
    public async Task<IActionResult> PostKoboldAISettingsAsync([FromRoute] Guid serviceId, [FromForm] KoboldAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("KoboldAISettings", value);
        }
        
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("KoboldAISettings", new { serviceId });
    }
    
    [HttpPost("/settings/koboldai/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteKoboldAISettingsAsync([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }

    [HttpGet("/settings/textgenerationinference/{serviceId:guid}")]
    public async Task<IActionResult> TextGenerationInferenceSettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
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
    public async Task<IActionResult> PostTextGenerationInferenceSettingsAsync([FromRoute] Guid serviceId, [FromForm] TextGenerationInferenceSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("TextGenerationInferenceSettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("TextGenerationInferenceSettings", new { serviceId });
    }
    
    [HttpPost("/settings/textgenerationinference/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteTextGenerationInferenceSettingsAsync([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/novelai/{serviceId:guid}")]
    public async Task<IActionResult> NovelAISettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
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
    public async Task<IActionResult> PostNovelAISettingsAsync([FromRoute] Guid serviceId, [FromForm] NovelAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("NovelAISettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId, _encryptionProvider);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("NovelAISettings", new { serviceId });
    }

    [HttpPost("/settings/novelai/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteNovelAISettingsAsync([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);

        return RedirectToAction("Settings", "Settings");
    }

    [HttpGet("/settings/openai/{serviceId:guid}")]
    public async Task<IActionResult> OpenAISettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
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
    public async Task<IActionResult> PostOpenAISettingsAsync([FromRoute] Guid serviceId, [FromForm] OpenAISettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("OpenAISettings", value);
        }

        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId, _encryptionProvider);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("OpenAISettings", new { serviceId });
    }
    
    [HttpPost("/settings/openai/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteOpenAISettingsAsync([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/windowsspeech/{serviceId:guid}")]
    public async Task<IActionResult> WindowsSpeechSettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
    
        #if(WINDOWS)
        var settings = await _servicesRepository.GetAsync<WindowsSpeechSettings>(serviceId, cancellationToken) ?? new ConfiguredService<WindowsSpeechSettings>
        {
            Id = serviceId,
            ServiceName = WindowsSpeechConstants.ServiceName,
            Settings = new WindowsSpeechSettings()
        };
        var vm = new WindowsSpeechSettingsViewModel(settings);
        return View(vm);
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/windowsspeech/{serviceId:guid}")]
    public async Task<IActionResult> PostWindowsSpeechSettingsAsync([FromRoute] Guid serviceId, [FromForm] WindowsSpeechSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("WindowsSpeechSettings", value);
        }

        #if(WINDOWS)
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = value.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("WindowsSpeechSettings", new { serviceId });
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/windowsspeech/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteWindowsSpeechSettingsAsync([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }
    
    [HttpGet("/settings/ffmpeg/{serviceId:guid}")]
    public async Task<IActionResult> FFmpegSettingsAsync([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        #if(!WINDOWS)
        var settings = await _servicesRepository.GetAsync<FFmpegSettings>(serviceId, cancellationToken) ?? new ConfiguredService<FFmpegSettings>
        {
            Id = serviceId,
            ServiceName = FFmpegConstants.ServiceName,
            Settings = new FFmpegSettings(),
        };
        var vm = new FFmpegSettingsViewModel(settings);
        return View(vm);
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/ffmpeg/{serviceId:guid}")]
    public async Task<IActionResult> PostFFmpegSettingsAsync([FromRoute] Guid serviceId, [FromForm] FFmpegSettingsViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("FFmpegSettings", value);
        }
        #if(!WINDOWS)
        if (serviceId == Guid.Empty)
            serviceId = Crypto.CreateCryptographicallySecureGuid();

        var settings = FFmpegSettingsViewModel.ToSettings(serviceId);
        await _servicesRepository.SaveAsync(settings);
        await UpdateProfileAsync(settings);
        
        return RedirectToAction("FFmpegSettings", new { serviceId });
        #else
        throw new PlatformNotSupportedException();
        #endif
    }
    
    [HttpPost("/settings/ffmpeg/{serviceId:guid}/delete")]
    public async Task<IActionResult> DeleteFFmpegSettingsAsync([FromRoute] Guid serviceId)
    {
        await _servicesRepository.DeleteAsync(serviceId);
        
        return RedirectToAction("Settings", "Settings");
    }

    private async Task UpdateProfileAsync(ConfiguredService settings)
    {
        var profile = await _profileRepository.GetRequiredProfileAsync(CancellationToken.None);
        var definition = _serviceRegistry.Get(settings.ServiceName) ?? throw new InvalidOperationException("No such service type");
        if (profile.EnsureService(settings, definition))
            await _profileRepository.SaveProfileAsync(profile);
    }
}