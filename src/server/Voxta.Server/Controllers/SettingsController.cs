using System.Text.Json;
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
using Voxta.Host.AspNetCore.WebSockets.Utils;
using Voxta.Services.AzureSpeechService;
using Voxta.Services.Fakes;

namespace Voxta.Server.Controllers;

[Controller]
public class SettingsController : Controller
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly DiagnosticsUtil _diagnosticsUtil;

    public SettingsController(ISettingsRepository settingsRepository, IProfileRepository profileRepository, DiagnosticsUtil diagnosticsUtil)
    {
        _settingsRepository = settingsRepository;
        _profileRepository = profileRepository;
        _diagnosticsUtil = diagnosticsUtil;
    }
    
    [HttpGet("/settings")]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var vm = new SettingsViewModel
        {
            Profile = await _profileRepository.GetProfileAsync(cancellationToken) ?? new ProfileSettings
            {
                Name = "User",
                Services = new ProfileSettings.ProfileServicesMap
                {
                    SpeechToText = new ServiceMap { Service = FakesConstants.ServiceName },
                    ActionInference = new ServiceMap { Service = FakesConstants.ServiceName }
                }
            },
            Services = (await _diagnosticsUtil.GetAllServicesAsync(cancellationToken)).Services,
        };
        
        return View(vm);
    }
    
    [HttpPost("/settings/test")]
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
    public async Task<IActionResult> TestAllSettings([FromForm] bool test, CancellationToken cancellationToken)
    {
        if (!test) throw new InvalidOperationException("Unexpected settings test without test flag.");
        
        var profile = await _profileRepository.GetProfileAsync(cancellationToken) ?? new ProfileSettings
        {
            Name = "User",
            Services = new ProfileSettings.ProfileServicesMap
            {
                SpeechToText = new ServiceMap { Service = FakesConstants.ServiceName },
                ActionInference = new ServiceMap { Service = FakesConstants.ServiceName }
            }
        };

        SettingsViewModel vm;
        try
        {
            var result = await _diagnosticsUtil.TestAllServicesAsync(cancellationToken);
            vm = new SettingsViewModel
            {
                Profile = profile,
                Services = result.Services,
            };
        }
        catch (Exception exc)
        {
            vm = new SettingsViewModel
            {
                Profile = profile,
                Services = new List<ServiceDiagnosticsResult>
                {
                    new()
                    {
                        ServiceName = "",
                        Label = $"Diagnostics Error: {exc.Message}",
                        IsHealthy = false,
                        IsReady = false,
                        Status = exc.ToString()
                    }
                }
            };
        }

        return View("Settings", vm);
    }
    
    [HttpGet("/settings/profile")]
    public async Task<IActionResult> ProfileSettings(CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken) ?? new ProfileSettings
        {
            Name = "User",
            Description = "",
            PauseSpeechRecognitionDuringPlayback = true,
            Services = new ProfileSettings.ProfileServicesMap
            {
                ActionInference = new ServiceMap
                {
                    Service = OpenAIConstants.ServiceName,
                },
                SpeechToText = new ServiceMap
                {
                    Service = VoskConstants.ServiceName,
                }
            }
        };
        return View(profile);
    }
    
    [HttpPost("/settings/profile")]
    public async Task<IActionResult> PostProfileSettings([FromForm] ProfileSettings value)
    {
        if (!ModelState.IsValid)
        {
            return View("ProfileSettings", value);
        }
        
        await _profileRepository.SaveProfileAsync(new ProfileSettings
        {
            Name = value.Name.TrimCopyPasteArtefacts(),
            Description = value.Description?.TrimCopyPasteArtefacts(),
            PauseSpeechRecognitionDuringPlayback = value.PauseSpeechRecognitionDuringPlayback,
            Services = value.Services
        });
        
        return RedirectToAction("Settings");
    }
    
    [HttpGet("/settings/azurespeechservice")]
    public async Task<IActionResult> AzureSpeechServiceSettings(CancellationToken cancellationToken)
    {
        var azurespeechservice = await _settingsRepository.GetAsync<AzureSpeechServiceSettings>(cancellationToken) ?? new AzureSpeechServiceSettings
        {
            SubscriptionKey = "",
            Region = "",
        };
        if (!string.IsNullOrEmpty(azurespeechservice.SubscriptionKey)) azurespeechservice.SubscriptionKey = Crypto.DecryptString(azurespeechservice.SubscriptionKey);  
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
            SubscriptionKey = string.IsNullOrEmpty(value.SubscriptionKey) ? "" : Crypto.EncryptString(value.SubscriptionKey.TrimCopyPasteArtefacts()),
            Region = value.Region.TrimCopyPasteArtefacts(),
            LogFilename = value.LogFilename?.TrimCopyPasteArtefacts(),
            FilterProfanity = value.FilterProfanity,
        });
        
        return RedirectToAction("Settings");
    }
    
    [HttpGet("/settings/vosk")]
    public async Task<IActionResult> VoskSettings(CancellationToken cancellationToken)
    {
        var vosk = await _settingsRepository.GetAsync<VoskSettings>(cancellationToken) ?? new VoskSettings
        {
            Model = "vosk-model-small-en-us-0.15",
            ModelHash = "30f26242c4eb449f948e42cb302dd7a686cb29a3423a8367f99ff41780942498",
        };
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
            Model = value.Model.TrimCopyPasteArtefacts(),
            ModelHash = value.ModelHash?.TrimCopyPasteArtefacts() ?? "",
        });
        
        return RedirectToAction("Settings");
    }
    
    [HttpGet("/settings/elevenlabs")]
    public async Task<IActionResult> ElevenLabsSettings(CancellationToken cancellationToken)
    {
        var elevenlabs = await _settingsRepository.GetAsync<ElevenLabsSettings>(cancellationToken) ?? new ElevenLabsSettings
        {
            ApiKey = "",
        };
        if (!string.IsNullOrEmpty(elevenlabs.ApiKey)) elevenlabs.ApiKey = Crypto.DecryptString(elevenlabs.ApiKey);  
        return View(elevenlabs);
    }
    
    [HttpPost("/settings/elevenlabs")]
    public async Task<IActionResult> PostElevenLabsSettings([FromForm] ElevenLabsSettings value)
    {
        if (!ModelState.IsValid)
        {
            return View("ElevenLabsSettings", value);
        }

        await _settingsRepository.SaveAsync(new ElevenLabsSettings
        {
            ApiKey = string.IsNullOrEmpty(value.ApiKey) ? "" : Crypto.EncryptString(value.ApiKey.TrimCopyPasteArtefacts()),
        });
        
        return RedirectToAction("Settings");
    }
    
    [HttpGet("/settings/textgenerationwebui")]
    public async Task<IActionResult> TextGenerationWebUISettings(CancellationToken cancellationToken)
    {
        var oobabooga = await _settingsRepository.GetAsync<OobaboogaSettings>(cancellationToken) ?? new OobaboogaSettings
        {
            Uri = "http://127.0.0.1:5000",
        };
        return View(oobabooga);
    }
    
    [HttpPost("/settings/textgenerationwebui")]
    public async Task<IActionResult> PostTextGenerationWebUISettings([FromForm] OobaboogaSettings value)
    {
        if (!ModelState.IsValid)
        {
            return View("TextGenerationWebUISettings", value);
        }

        await _settingsRepository.SaveAsync(new OobaboogaSettings
        {
            Uri = value.Uri.TrimCopyPasteArtefacts(),
        });
        
        return RedirectToAction("Settings");
    }
    
    [HttpGet("/settings/koboldai")]
    public async Task<IActionResult> KoboldAISettings(CancellationToken cancellationToken)
    {
        var koboldai = await _settingsRepository.GetAsync<KoboldAISettings>(cancellationToken) ?? new KoboldAISettings
        {
            Uri = "http://127.0.0.1:5001",
        };
        return View(koboldai);
    }
    
    [HttpPost("/settings/koboldai")]
    public async Task<IActionResult> PostKoboldAISettings([FromForm] KoboldAISettings value)
    {
        if (!ModelState.IsValid)
        {
            return View("KoboldAISettings", value);
        }

        await _settingsRepository.SaveAsync(new KoboldAISettings
        {
            Uri = value.Uri.TrimCopyPasteArtefacts(),
        });
        
        return RedirectToAction("Settings");
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
            Parameters = JsonSerializer.Serialize(novelai.Parameters ?? new NovelAIParameters()),
            UseDefaults = novelai.Parameters == null,
            Token = !string.IsNullOrEmpty(novelai.Token) ? Crypto.DecryptString(novelai.Token) : ""
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
            Token = string.IsNullOrEmpty(value.Token) ? "" : Crypto.EncryptString(value.Token.TrimCopyPasteArtefacts()),
            Model = value.Model.TrimCopyPasteArtefacts(),
            Parameters = value.UseDefaults ? null : JsonSerializer.Deserialize<NovelAIParameters>(value.Parameters) ?? new NovelAIParameters()
        });
        
        return RedirectToAction("Settings");
    }
    
    [HttpGet("/settings/openai")]
    public async Task<IActionResult> OpenAISettings(CancellationToken cancellationToken)
    {
        var openai = await _settingsRepository.GetAsync<OpenAISettings>(cancellationToken) ?? new OpenAISettings
        {
            ApiKey = "",
        };
        if (!string.IsNullOrEmpty(openai.ApiKey)) openai.ApiKey = Crypto.DecryptString(openai.ApiKey);  
        return View(openai);
    }
    
    [HttpPost("/settings/openai")]
    public async Task<IActionResult> PostOpenAISettings([FromForm] OpenAISettings value)
    {
        if (!ModelState.IsValid)
        {
            return View("OpenAISettings", value);
        }

        await _settingsRepository.SaveAsync(new OpenAISettings
        {
            ApiKey = string.IsNullOrEmpty(value.ApiKey) ? "" : Crypto.EncryptString(value.ApiKey.TrimCopyPasteArtefacts()),
            Model = value.Model.TrimCopyPasteArtefacts(),
        });
        
        return RedirectToAction("Settings");
    }
}