using System.Diagnostics.CodeAnalysis;
using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatServicesLocator
{
    public readonly ISelectorFactory<ITextGenService> TextGenFactory;
    public readonly ISelectorFactory<ITextToSpeechService> TextToSpeechFactory;
    public readonly PendingSpeechManager PendingSpeech;
    public readonly ISelectorFactory<IAnimationSelectionService> AnimSelectFactory;
    private readonly ILogger<ChatSession> _logger;
    public readonly IBotRepository BotsRepository;
    public readonly IProfileRepository ProfileRepository;
    public readonly LocalInputEventDispatcher LocalInputEventDispatcher;

    [SuppressMessage("ReSharper", "ContextualLoggerProblem")]
    public ChatServicesLocator(
        ISelectorFactory<ITextGenService> textGenFactory,
        ISelectorFactory<ITextToSpeechService> textToSpeechFactory,
        PendingSpeechManager pendingSpeech,
        ISelectorFactory<IAnimationSelectionService> animSelectFactory,
        ILogger<ChatSession> logger,
        IBotRepository botsRepository,
        IProfileRepository profileRepository,
        LocalInputEventDispatcher localInputEventDispatcher
    )
    {
        TextGenFactory = textGenFactory;
        TextToSpeechFactory = textToSpeechFactory;
        PendingSpeech = pendingSpeech;
        AnimSelectFactory = animSelectFactory;
        _logger = logger;
        BotsRepository = botsRepository;
        ProfileRepository = profileRepository;
        LocalInputEventDispatcher = localInputEventDispatcher;
    }
}