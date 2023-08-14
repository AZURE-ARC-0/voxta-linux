using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Tokenizers;
using Voxta.Shared.LLMUtils;

namespace Voxta.Shared.RemoteServicesUtils;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public abstract class LLMServiceClientBase<TSettings, TInputParameters, TOutputParameters>
    where TSettings : LLMSettingsBase<TInputParameters> where TInputParameters : new()
{   
    private static readonly IMapper Mapper;
    
    protected abstract ITokenizer Tokenizer { get; }
    
    static LLMServiceClientBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TInputParameters, TOutputParameters>();
        });
        Mapper = config.CreateMapper();
    }


    public string ServiceName { get; }
    public string[] Features => new[] { ServiceFeatures.NSFW, ServiceFeatures.GPT3 };

    private LLMSettingsBase<TInputParameters>? _settings;

    protected LLMSettingsBase<TInputParameters> Settings
    {
        get => _settings ?? throw new NullReferenceException("Settings were not loaded prior to calling this property");
        private set => _settings = value;
    }
    
    protected TInputParameters? Parameters { get; private set; }
    
    
    private readonly ISettingsRepository _settingsRepository;

    protected LLMServiceClientBase(string serviceName, ISettingsRepository settingsRepository)
    {
        ServiceName = serviceName;
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> TryInitializeAsync(string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<TSettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        Parameters = settings.Parameters ?? new TInputParameters();
        Settings = settings;
        return await TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken);
    }

    protected abstract Task<bool> TryInitializeAsync(TSettings settings, string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken);

    public int GetTokenCount(string message)
    {
        return Tokenizer.CountTokens(message);
    }

    protected TOutputParameters CreateParameters()
    {
        return Mapper.Map<TOutputParameters>(Parameters);
    }

    public void Dispose()
    {
    }
}