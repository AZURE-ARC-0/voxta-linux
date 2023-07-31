using Voxta.Abstractions.Model;
using Voxta.Services.AzureSpeechService;
using Voxta.Services.ElevenLabs;
using Voxta.Services.KoboldAI;
using Voxta.Services.NovelAI;
using Voxta.Services.Oobabooga;
using Voxta.Services.OpenAI;
using Voxta.Services.Vosk;
using Voxta.Services.WindowsSpeech;

namespace Voxta.Host.AspNetCore.WebSockets.Utils;

public static class ProfileUtils
{
    public static ProfileSettings CreateDefaultProfile()
    {
        return new ProfileSettings
        {
            Name = "User",
            TextGen =
            {
                Services = new[]
                {
                    OobaboogaConstants.ServiceName,
                    KoboldAIConstants.ServiceName,
                    NovelAIConstants.ServiceName,
                    OpenAIConstants.ServiceName,
                }
            },
            SpeechToText =
            {
                Services = new[]
                {
                    AzureSpeechServiceConstants.ServiceName,
#if(WINDOWS)
                    WindowsSpeechConstants.ServiceName,
#endif
                    VoskConstants.ServiceName,
                }
            },
            TextToSpeech =
            {
                Services = new[]
                {
                    NovelAIConstants.ServiceName,
                    ElevenLabsConstants.ServiceName,
                    AzureSpeechServiceConstants.ServiceName,
#if(WINDOWS)
                    WindowsSpeechConstants.ServiceName,
#endif
                }
            },
            ActionInference =
            {
                Services = new[]
                {
                    OpenAIConstants.ServiceName,
                    OobaboogaConstants.ServiceName
                }
            }
        };
    }
}