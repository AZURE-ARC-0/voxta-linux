using Voxta.Abstractions.Management;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Voxta.Core;

public interface ISpeechGenerator : IDisposable
{
    string ServiceName { get; }
    string? Voice { get; }
    Task<string?> CreateSpeechAsync(string text, string id, bool reusable, CancellationToken cancellationToken);
}

public class SpeechGeneratorFactory
{
    private readonly IServiceProvider _sp;

    public SpeechGeneratorFactory(IServiceProvider sp)
    {
        _sp = sp;
    }
    
    public ISpeechGenerator Create(ITextToSpeechService? service, string? ttsVoice, string culture, string? audioPath, string[] acceptContentTypes, CancellationToken cancellationToken)
    {
        if (service == null || string.IsNullOrEmpty(ttsVoice))
            return new NoSpeechGenerator();
        
        var audioConverter = _sp.GetRequiredService<IAudioConverter>();
        audioConverter.SelectOutputContentType(acceptContentTypes, service.ContentType);

        if (audioPath == null)
            return new RemoteSpeechGenerator(service.ServiceName, ttsVoice, culture, _sp.GetRequiredService<PendingSpeechManager>(), audioConverter.ContentType);

        return new LocalSpeechGenerator(service, ttsVoice, culture, _sp.GetRequiredService<ITemporaryFileCleanup>(), audioPath, audioConverter);
    }
}

public class NoSpeechGenerator : ISpeechGenerator
{
    public string ServiceName => "None";
    public string Voice => "None";
    
    public Task<string?> CreateSpeechAsync(string text, string id, bool reusable, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }

    public void Dispose()
    {
    }
}

public class LocalSpeechGenerator : ISpeechGenerator
{
    public string ServiceName => _textToSpeechService.ServiceName;
    public string Voice => _ttsVoice;
    
    private readonly ITextToSpeechService _textToSpeechService;
    private readonly string _ttsVoice;
    private readonly ITemporaryFileCleanup _temporaryFileCleanup;
    private readonly string _audioPath;
    private readonly IAudioConverter _audioConverter;
    private readonly string _culture;

    public LocalSpeechGenerator(ITextToSpeechService textToSpeechService, string ttsVoice, string culture, ITemporaryFileCleanup temporaryFileCleanup, string audioPath, IAudioConverter audioConverter)
    {
        _textToSpeechService = textToSpeechService;
        _ttsVoice = ttsVoice;
        _culture = culture;
        _temporaryFileCleanup = temporaryFileCleanup;
        _audioPath = audioPath;
        _audioConverter = audioConverter;
    }
    
    public async Task<string?> CreateSpeechAsync(string text, string id, bool reusable, CancellationToken cancellationToken)
    {
        var speechUrl = Path.Combine(_audioPath, $"{id}.{AudioData.GetExtension(_audioConverter.ContentType)}");
        var speechTunnel = new ConversionSpeechTunnel(new FileSpeechTunnel(speechUrl), _audioConverter);
        if (File.Exists(speechUrl)) return speechUrl;
        if (!reusable)
            _temporaryFileCleanup.MarkForDeletion(speechUrl, false);
        await _textToSpeechService.GenerateSpeechAsync(new SpeechRequest
            {
                Service = _textToSpeechService.ServiceName,
                Text = text,
                Voice = _ttsVoice,
                Culture = _culture,
                ContentType = _audioConverter.ContentType,
            },
            speechTunnel,
            cancellationToken
        );
        return speechUrl;
    }

    public void Dispose()
    {
        _textToSpeechService.Dispose();
    }
}

public class RemoteSpeechGenerator : ISpeechGenerator
{
    public string ServiceName => _ttsService;
    public string Voice => _ttsVoice;
    
    private readonly string _ttsService;
    private readonly string _ttsVoice;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly string _contentType;
    private readonly string _culture;

    public RemoteSpeechGenerator(string ttsService, string ttsVoice, string culture, PendingSpeechManager pendingSpeech, string contentType)
    {
        _ttsService = ttsService;
        _ttsVoice = ttsVoice;
        _culture = culture;
        _pendingSpeech = pendingSpeech;
        _contentType = contentType;
    }

    public Task<string?> CreateSpeechAsync(string text, string id, bool reusable, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(CreateSpeechUrl(Crypto.CreateCryptographicallySecureGuid().ToString(), text, _ttsService, _ttsVoice, reusable));
    }

    private string CreateSpeechUrl(string id, string text, string ttsService, string ttsVoice, bool reusable)
    {
        _pendingSpeech.Push(id, new SpeechRequest
        {
            Service = ttsService,
            Text = text,
            Voice = ttsVoice,
            Culture = _culture,
            ContentType = _contentType,
            Reusable = reusable,
        });
        var speechUrl = $"/tts/gens/{id}.{AudioData.GetExtension(_contentType)}";
        return speechUrl;
    }

    public void Dispose()
    {
    }
}