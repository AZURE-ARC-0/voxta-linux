using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Core;

public interface ISpeechGenerator
{
    Task<string?> CreateSpeechAsync(string text, string id, CancellationToken cancellationToken);
}

public class SpeechGeneratorFactory
{
    private readonly IServiceProvider _sp;

    public SpeechGeneratorFactory(IServiceProvider sp)
    {
        _sp = sp;
    }
    
    public async Task<ISpeechGenerator> CreateAsync(string? ttsService, string? ttsVoice, string? audioPath, string[] acceptContentTypes, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ttsService) || string.IsNullOrEmpty(ttsVoice))
            return new NoSpeechGenerator();
        
        var textToSpeech = await _sp.GetRequiredService<IServiceFactory<ITextToSpeechService>>().CreateAsync(ttsService, cancellationToken);
        
        var audioConverter = _sp.GetRequiredService<IAudioConverter>();
        audioConverter.SelectOutputContentType(acceptContentTypes, textToSpeech.ContentType);

        if (audioPath == null)
            return new RemoteSpeechGenerator(ttsService, ttsVoice, _sp.GetRequiredService<PendingSpeechManager>(), audioConverter.ContentType);

        return new LocalSpeechGenerator(textToSpeech, ttsVoice, _sp.GetRequiredService<ITemporaryFileCleanup>(), audioPath, audioConverter);
    }
}

public class NoSpeechGenerator : ISpeechGenerator
{
    public Task<string?> CreateSpeechAsync(string text, string id, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }
}

public class LocalSpeechGenerator : ISpeechGenerator
{
    private readonly ITextToSpeechService _textToSpeechService;
    private readonly string _ttsVoice;
    private readonly ITemporaryFileCleanup _temporaryFileCleanup;
    private readonly string _audioPath;
    private readonly IAudioConverter _audioConverter;

    public LocalSpeechGenerator(ITextToSpeechService textToSpeechService, string ttsVoice, ITemporaryFileCleanup temporaryFileCleanup, string audioPath, IAudioConverter audioConverter)
    {
        _textToSpeechService = textToSpeechService;
        _ttsVoice = ttsVoice;
        _temporaryFileCleanup = temporaryFileCleanup;
        _audioPath = audioPath;
        _audioConverter = audioConverter;
    }
    
    public async Task<string?> CreateSpeechAsync(string text, string id, CancellationToken cancellationToken)
    {
        var speechUrl = Path.Combine(_audioPath, $"{id}.{AudioData.GetExtension(_audioConverter.ContentType)}");
        var speechTunnel = new ConversionSpeechTunnel(new FileSpeechTunnel(speechUrl), _audioConverter);
        if (File.Exists(speechUrl)) return speechUrl;
        _temporaryFileCleanup.MarkForDeletion(speechUrl);
        await _textToSpeechService.GenerateSpeechAsync(new SpeechRequest
            {
                Service = _textToSpeechService.ServiceName,
                Text = text,
                Voice = _ttsVoice,
                ContentType = _audioConverter.ContentType,
            },
            speechTunnel,
            cancellationToken
        );
        return speechUrl;
    }
}

public class RemoteSpeechGenerator : ISpeechGenerator
{
    private readonly string _ttsService;
    private readonly string _ttsVoice;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly string _contentType;

    public RemoteSpeechGenerator(string ttsService, string ttsVoice, PendingSpeechManager pendingSpeech, string contentType)
    {
        _ttsService = ttsService;
        _ttsVoice = ttsVoice;
        _pendingSpeech = pendingSpeech;
        _contentType = contentType;
    }

    public Task<string?> CreateSpeechAsync(string text, string id, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(CreateSpeechUrl(Crypto.CreateCryptographicallySecureGuid().ToString(), text, _ttsService, _ttsVoice));
    }

    private string CreateSpeechUrl(string id, string text, string ttsService, string ttsVoice)
    {
        _pendingSpeech.Push(id, new SpeechRequest
        {
            Service = ttsService,
            Text = text,
            Voice = ttsVoice,
            ContentType = _contentType,
        });
        var speechUrl = $"/tts/gens/{id}.{AudioData.GetExtension(_contentType)}";
        return speechUrl;
    }
}