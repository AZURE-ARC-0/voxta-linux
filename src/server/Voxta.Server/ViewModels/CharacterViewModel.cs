﻿using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels;

public class CharacterViewModel
{
    public required Character Character { get; init; }
    public required bool PrerequisiteNSFW { get; set; }
}

public class CharacterViewModelWithOptions : CharacterViewModel
{
    public required VoiceInfo[] Voices { get; set; } = Array.Empty<VoiceInfo>();
    public required OptionViewModel[] TextGenServices { get; init; }
    public required OptionViewModel[] TextToSpeechServices { get; init; }
    public required bool IsNew { get; set; }
}