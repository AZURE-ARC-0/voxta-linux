using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.Tokenizers;

namespace Voxta.Shared.LLMUtils;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public abstract class LLMServiceClientBase<TSettings, TInputParameters, TOutputParameters> : ServiceBase<TSettings>
    where TSettings : LLMSettingsBase<TInputParameters>
    where TInputParameters : new()
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
    
    public string[] Features => new[] { ServiceFeatures.NSFW };

    private TSettings? _settings;

    protected TSettings Settings
    {
        get => _settings ?? throw new NullReferenceException("Settings were not loaded prior to calling this property");
        private set => _settings = value;
    }
    
    protected TInputParameters? Parameters { get; private set; }
    
    protected LLMServiceClientBase(ISettingsRepository settingsRepository)
        : base(settingsRepository)
    {
    }

    protected override async Task<bool> TryInitializeAsync(TSettings settings, string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;

        Parameters = settings.Parameters ?? CreateDefaultParameters(settings);
        Settings = settings;
        return true;
    }

    protected virtual TInputParameters CreateDefaultParameters(TSettings settings)
    {
        return new TInputParameters();
    }

    public int GetTokenCount(string message)
    {
        return Tokenizer.CountTokens(message);
    }

    public (List<ChatMessageData> Messages, int Tokens)? GetMessagesToSummarize(IChatInferenceData chat)
    {
        if (chat.GetMessages().Sum(m => m.Tokens) < Settings.SummarizationTriggerTokens)
            return null;
        
        var messagesTokens = 0;
        var messagesToSummarize = new List<ChatMessageData>();
        foreach (var message in chat.GetMessages())
        {
            if (messagesTokens + message.Tokens > Settings.SummarizationDigestTokens) break;
            messagesTokens += message.Tokens;
            messagesToSummarize.Add(message);
        }

        if (messagesToSummarize.Count == 0)
            throw new InvalidOperationException("Cannot summarize, not enough tokens for a single message");

        return (messagesToSummarize, messagesTokens);
    }

    protected TOutputParameters CreateParameters()
    {
        return Mapper.Map<TOutputParameters>(Parameters);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}