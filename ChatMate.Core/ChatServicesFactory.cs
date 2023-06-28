using System.Diagnostics.CodeAnalysis;
using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatServicesFactory
{
    public readonly ISelectorFactory<ITextGenService> _textGenFactory;
    public readonly ISelectorFactory<ITextToSpeechService> _textToSpeechFactory;
    public readonly PendingSpeechManager _pendingSpeech;
    public readonly ISelectorFactory<IAnimationSelectionService> _animSelectFactory;
    private readonly ILogger<ChatSession> _logger;
    public readonly IBotRepository _bots;
    public readonly IProfileRepository _profileRepository;

    [SuppressMessage("ReSharper", "ContextualLoggerProblem")]
    public ChatServicesFactory(ISelectorFactory<ITextGenService> textGenFactory, ISelectorFactory<ITextToSpeechService> textToSpeechFactory, PendingSpeechManager pendingSpeech, ISelectorFactory<IAnimationSelectionService> animSelectFactory, ILogger<ChatSession> logger, IBotRepository bots, IProfileRepository profileRepository)
    {
        _textGenFactory = textGenFactory;
        _textToSpeechFactory = textToSpeechFactory;
        _pendingSpeech = pendingSpeech;
        _animSelectFactory = animSelectFactory;
        _logger = logger;
        _bots = bots;
        _profileRepository = profileRepository;
    }
}