using Voxta.Abstractions.Management;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Voxta.Core;

public interface ISpeechGenerator : IDisposable
{
    ServiceLink? Link { get; }
    string? Voice { get; }
    Task<string?> CreateSpeechAsync(string text, string id, bool reusable, CancellationToken cancellationToken);
    Task<string?> LoadSpeechAsync(string file, string thinkingSpeechId, bool b, CancellationToken cancellationToken);
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
        if (service == null)
            return new NoSpeechGenerator();
        
        var audioConverter = _sp.GetRequiredService<IAudioConverter>();
        audioConverter.SelectOutputContentType(acceptContentTypes, service.ContentType);

        if (audioPath == null)
            return new RemoteSpeechGenerator(service.SettingsRef, ttsVoice ?? "", culture, _sp.GetRequiredService<PendingSpeechManager>(), audioConverter.ContentType);

        return new LocalSpeechGenerator(service, ttsVoice ?? "", culture, _sp.GetRequiredService<ITemporaryFileCleanup>(), audioPath, audioConverter);
    }
}

public class NoSpeechGenerator : ISpeechGenerator
{
    public ServiceLink? Link => null;
    public string Voice => "None";
    
    public Task<string?> CreateSpeechAsync(string text, string id, bool reusable, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }
    
    public Task<string?> LoadSpeechAsync(string file, string id, bool reusable, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }

    public void Dispose()
    {
    }
}

public class LocalSpeechGenerator : ISpeechGenerator
{
    public ServiceLink Link => _textToSpeechService.SettingsRef.ToLink();
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
                ServiceName = _textToSpeechService.SettingsRef.ServiceName,
                ServiceId = _textToSpeechService.SettingsRef.ServiceId,
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
    
    public async Task<string?> LoadSpeechAsync(string file, string id, bool reusable, CancellationToken cancellationToken)
    {
        var speechUrl = Path.Combine(_audioPath, $"{id}.{AudioData.GetExtension(_audioConverter.ContentType)}");
        var speechTunnel = new ConversionSpeechTunnel(new FileSpeechTunnel(speechUrl), _audioConverter);
        if (File.Exists(speechUrl)) return speechUrl;
        if (!reusable)
            _temporaryFileCleanup.MarkForDeletion(speechUrl, false);
        var audioData = new AudioData(File.OpenRead(file), AudioData.FromExtension(Path.GetExtension(file)));
        await speechTunnel.SendAsync(audioData, cancellationToken);
        return speechUrl;
    }

    public void Dispose()
    {
        _textToSpeechService.Dispose();
    }
}

public class RemoteSpeechGenerator : ISpeechGenerator
{
    public ServiceLink Link => _ttsService.ToLink();
    public string Voice => _ttsVoice;
    
    private readonly ServiceSettingsRef _ttsService;
    private readonly string _ttsVoice;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly string _contentType;
    private readonly string _culture;

    public RemoteSpeechGenerator(ServiceSettingsRef ttsService, string ttsVoice, string culture, PendingSpeechManager pendingSpeech, string contentType)
    {
        _ttsService = ttsService;
        _ttsVoice = ttsVoice;
        _culture = culture;
        _pendingSpeech = pendingSpeech;
        _contentType = contentType;
    }

    public Task<string?> CreateSpeechAsync(string text, string id, bool reusable, CancellationToken cancellationToken)
    {
        var pendingId = Crypto.CreateCryptographicallySecureGuid().ToString();
        _pendingSpeech.Push(pendingId, new SpeechRequest
        {
            ServiceName = _ttsService.ServiceName,
            ServiceId = _ttsService.ServiceId,
            Text = text,
            Voice = _ttsVoice,
            Culture = _culture,
            ContentType = _contentType,
            Reusable = reusable,
        });
        var speechUrl = $"/tts/gens/{pendingId}.{AudioData.GetExtension(_contentType)}";
        return Task.FromResult<string?>(speechUrl);
    }
    
    public Task<string?> LoadSpeechAsync(string file, string id, bool reusable, CancellationToken cancellationToken)
    {
        // Get the relative path of file from the root of the audio directory
        var relativePath = Path.GetRelativePath("Data/Audio", file);
        var speechUrl = $"/tts/file?path={relativePath.Replace('\\', '/')}&contentType={_contentType}";
        return Task.FromResult<string?>(speechUrl);
    }

    public void Dispose()
    {
    }
}