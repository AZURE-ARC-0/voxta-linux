using System.Diagnostics.CodeAnalysis;
using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;

namespace ChatMate.Core;

public class ChatServicesLocator
{
    public readonly ISelectorFactory<ITextGenService> TextGenFactory;
    public readonly ISelectorFactory<ITextToSpeechService> TextToSpeechFactory;
    public readonly PendingSpeechManager PendingSpeech;
    public readonly ISelectorFactory<IAnimationSelectionService> AnimSelectFactory;
    public readonly IBotRepository BotsRepository;
    public readonly IProfileRepository ProfileRepository;
    public readonly ExclusiveLocalInputManager ExclusiveLocalInputManager;
    public readonly ITemporaryFileCleanup TemporaryFileCleanup;

    [SuppressMessage("ReSharper", "ContextualLoggerProblem")]
    public ChatServicesLocator(
        ISelectorFactory<ITextGenService> textGenFactory,
        ISelectorFactory<ITextToSpeechService> textToSpeechFactory,
        PendingSpeechManager pendingSpeech,
        ISelectorFactory<IAnimationSelectionService> animSelectFactory,
        IBotRepository botsRepository,
        IProfileRepository profileRepository,
        ExclusiveLocalInputManager exclusiveLocalInputManager,
        ITemporaryFileCleanup temporaryFileCleanup
    )
    {
        TextGenFactory = textGenFactory;
        TextToSpeechFactory = textToSpeechFactory;
        PendingSpeech = pendingSpeech;
        AnimSelectFactory = animSelectFactory;
        BotsRepository = botsRepository;
        ProfileRepository = profileRepository;
        ExclusiveLocalInputManager = exclusiveLocalInputManager;
        TemporaryFileCleanup = temporaryFileCleanup;
    }
}