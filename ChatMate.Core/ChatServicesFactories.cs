using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;

namespace ChatMate.Core;

public class ChatServicesFactories
{
    public readonly ISelectorFactory<ITextGenService> TextGenFactory;
    public readonly ISelectorFactory<ITextToSpeechService> TextToSpeechFactory;

    public ChatServicesFactories(
        ISelectorFactory<ITextGenService> textGenFactory,
        ISelectorFactory<ITextToSpeechService> textToSpeechFactory
    )
    {
        TextGenFactory = textGenFactory;
        TextToSpeechFactory = textToSpeechFactory;
    }

    public ChatServices Create(string textGen, string? tts)
    {
        return new ChatServices
        {
            TextGen = TextGenFactory.Create(textGen),
            TextToSpeech = !string.IsNullOrEmpty(tts) ? TextToSpeechFactory.Create(tts) : null
        };
    }
}

public class ChatServices
{
    public required ITextGenService TextGen { get; init; }
    public ITextToSpeechService? TextToSpeech { get; init; }
}
