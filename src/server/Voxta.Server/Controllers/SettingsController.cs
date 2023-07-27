using System.Diagnostics.CodeAnalysis;
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
        var vm = await GetSettingsViewModel(() => _diagnosticsUtil.GetAllServicesAsync(cancellationToken), cancellationToken);
        return View(vm);
    }

    [HttpPost("/settings/test")]
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
    public async Task<IActionResult> TestAllSettings([FromForm] bool test, CancellationToken cancellationToken)
    {
        if (!test) throw new InvalidOperationException("Unexpected settings test without test flag.");
        var vm = await GetSettingsViewModel(() => _diagnosticsUtil.TestAllServicesAsync(cancellationToken), cancellationToken);
        return View("Settings", vm);
    }

    private async Task<SettingsViewModel> GetSettingsViewModel(Func<Task<DiagnosticsResult>> getServices, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken) ?? CreateDefaultProfile();
        var services = await getServices();
        var vm = new SettingsViewModel
        {
            Profile = profile,
            Services = new SettingsServiceViewModel[]
            {
                new()
                {
                    Name = "profile",
                    Title = "Profile",
                    Services = new[] { services.Profile },
                    Help = """
                        <p>Your profile defines how the AI will see you and call you.</p>
                        """
                },
                new()
                {
                    Name = "stt",
                    Title = "Speech To Text Services",
                    Services = services.SpeechToTextServices,
                    Help = """
                        <p>Azure Speech Service is highly recommended because it supports punctuation and works extremely well. It is also free up to a point. Otherwise, Vosk is free, fast and runs locally, despite being inferior to Azure.</p>
                        """
                },
                new()
                {
                    Name = "textgen",
                    Title = "Text Generation Services",
                    Services = services.TextGenServices,
                    Help = """
                        <p>Some recommendations:</p>
                        <ul>
                            <li>
                                <p><b>Easy, fast and unrestricted: NovelAI</b></p>
                                <p>For fast and natural speech with minimal setup, use NovelAI for both Text Gen and TTS.</p>
                            </li>
                            <li>
                                <p><b>Easy, fast, coherent but restricted: OpenAI</b></p>
                                <p>For a helpful AI that knows things and can really help with questions, use OpenAI for Text Gen. GPT-4 is very good, but also much slower and more expensive.</p>
                            </li>
                            <li>
                                <p><b>Unrestricted, but slower, requires a GPU and some efforts to setup: Text Generation Web UI or KoboldAI</b></p>
                                <p>For unrestricted chat, use Text Generation Web UI or KoboldAI for TextGen. You need to host them yourself or on RunPod.io (or another host), and they are more complicated to setup, but they won't cost you anything and they can run completely locally.</p>
                            </li>
                        </ul>
                        """
                },
                new()
                {
                    Name = "tts",
                    Title = "Text To Speech Services",
                    Services = services.TextToSpeechServices,
                    Help = """
                        <p>Some recommendations:</p>
                        <ul>
                            <li>
                                <p><b>The best available option for english: NovelAI</b></p>
                                <p>For fast and natural speech with minimal setup, use NovelAI for both Text Gen and TTS.</p>
                            </li>
                            <li>
                                <p><b>Excellent quality, slightly slower and expensive but supports other languages: ElevenLabs</b></p>
                                <p>ElevenLabs has an amazing quality with decent speed, but higher usage can get expensive.</p>
                            </li>
                            <li>
                                <p><b>Fair quality: Azure Speech Service</b></p>
                                <p>Azure Speech Service is fair, but not great. Still a good, free option for low use.</p>
                            </li>
                        </ul>
                        """
                },
                new()
                {
                    Name = "action_inference",
                    Title = "Action Inference Services",
                    Services = services.ActionInferenceServices,
                    Help = """
                        <p>You should use OpenAI, even for NSFW, unless you want to experiment with other LLMs. Keep in mind, the LLM must be good to do action inference correctly.</p>
                        """
                },
            }
        };
        return vm;
    }

    private static ProfileSettings CreateDefaultProfile()
    {
        return new ProfileSettings
        {
            Name = "User",
            TextGen =
            {
                Services = new[] { OobaboogaConstants.ServiceName, KoboldAIConstants.ServiceName, NovelAIConstants.ServiceName, OpenAIConstants.ServiceName }
            },
            SpeechToText =
            {
                Services = new[] { AzureSpeechServiceConstants.ServiceName, VoskConstants.ServiceName }
            },
            TextToSpeech =
            {
                Services = new[] { NovelAIConstants.ServiceName, ElevenLabsConstants.ServiceName, AzureSpeechServiceConstants.ServiceName }
            },
            ActionInference =
            {
                Services = new[] { OpenAIConstants.ServiceName, OobaboogaConstants.ServiceName }
            }
        };
    }

    [HttpPost("/settings/reorder")]
    public async Task<ActionResult> Reorder([FromForm] string type, [FromForm] string name, [FromForm] string direction)
    {
        var profile = await _profileRepository.GetRequiredProfileAsync(CancellationToken.None);
        switch (type)
        {
            case "stt":
                profile.SpeechToText = Reorder(profile.SpeechToText, name, direction);
                break;
            case "tts":
                profile.TextToSpeech = Reorder(profile.TextToSpeech, name, direction);
                break;
            case "action_inference":
                profile.ActionInference = Reorder(profile.ActionInference, name, direction);
                break;
            case "textgen":
                profile.TextGen = Reorder(profile.TextGen, name, direction);
                break;
            default:
                throw new NotSupportedException($"Cannot reorder {type}");
        }

        await _profileRepository.SaveProfileAsync(profile);
        return RedirectToAction("Settings");
    }

    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
    private static ServicesList Reorder(ServicesList services, string name, string direction)
    {
        if (direction != "up") throw new NotSupportedException("Only up is supported");
        var index = Array.IndexOf(services.Services, name);
        if (index <= 0) return services;
        var list = new List<string>(services.Services);
        list.RemoveAt(index);
        list.Insert(index - 1, name);
        services.Services = list.ToArray();
        return services;
    }

    [HttpGet("/settings/profile")]
    public async Task<IActionResult> ProfileSettings(CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetProfileAsync(cancellationToken) ?? CreateDefaultProfile();
        var vm = new ProfileViewModel
        {
            Name = profile.Name,
            Description = profile.Description ?? "",
            PauseSpeechRecognitionDuringPlayback = profile.PauseSpeechRecognitionDuringPlayback,
        };
        return View(vm);
    }
    
    [HttpPost("/settings/profile")]
    public async Task<IActionResult> PostProfileSettings([FromForm] ProfileViewModel value)
    {
        if (!ModelState.IsValid)
        {
            return View("ProfileSettings", value);
        }
        
        var profile = await _profileRepository.GetProfileAsync(CancellationToken.None) ?? CreateDefaultProfile();

        profile.Name = value.Name;
        profile.Description = value.Description;
        profile.PauseSpeechRecognitionDuringPlayback = value.PauseSpeechRecognitionDuringPlayback;

        await _profileRepository.SaveProfileAsync(profile);
        
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
            Enabled = value.Enabled,
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
            Enabled = value.Enabled,
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
            Enabled = value.Enabled,
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
        
        return RedirectToAction("Settings");
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
            Token = !string.IsNullOrEmpty(novelai.Token) ? Crypto.DecryptString(novelai.Token) : "",
            Parameters = JsonSerializer.Serialize(novelai.Parameters ?? new NovelAIParameters()),
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
            Token = string.IsNullOrEmpty(value.Token) ? "" : Crypto.EncryptString(value.Token.TrimCopyPasteArtefacts()),
            Model = value.Model.TrimCopyPasteArtefacts(),
            Parameters = value.UseDefaults ? null : JsonSerializer.Deserialize<NovelAIParameters>(value.Parameters) ?? new NovelAIParameters(),
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
            ApiKey = string.IsNullOrEmpty(value.ApiKey) ? "" : Crypto.EncryptString(value.ApiKey.TrimCopyPasteArtefacts()),
            Model = value.Model.TrimCopyPasteArtefacts(),
            Parameters = value.UseDefaults ? null : JsonSerializer.Deserialize<OpenAIParameters>(value.Parameters) ?? new OpenAIParameters(),
        });
        
        return RedirectToAction("Settings");
    }
}