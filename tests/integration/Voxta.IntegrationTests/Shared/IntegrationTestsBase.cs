using System.Globalization;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;
using Voxta.Core;
using Voxta.Data.LiteDB;
using Voxta.Security.Windows;

namespace Voxta.IntegrationTests.Shared;

public abstract class IntegrationTestsBase
{
    private static readonly LiteDatabase Db;
    private static readonly TestHttpClientFactory HttpClientFactory = new TestHttpClientFactory();

    static IntegrationTestsBase()
    {
        Db = new LiteDatabase(@"../../../../../../../src/server/Voxta.Server/Data/Voxta.db");
    }
    
    ~IntegrationTestsBase()
    {
        Db.Dispose();
        HttpClientFactory.Dispose();
    }
    
    protected TestServiceObserver ServiceObserver { get; private set; } = null!;

    [SetUp]
    public void Setup()
    {
        ServiceObserver = new TestServiceObserver();
    }

    protected async Task<TClient> CreateClientAsync<TClient>() where TClient : class, IService
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory>(_ => HttpClientFactory);
        services.AddSingleton(Db);
        services.AddSingleton<ISettingsRepository>(_ => new SettingsLiteDBRepository(Db));
        services.AddSingleton<IPerformanceMetrics>(_ => new TestPerformanceMetrics());
        services.AddSingleton<IServiceObserver>(ServiceObserver);
        services.AddSingleton<ILocalEncryptionProvider>(_ => new DPAPIEncryptionProvider());
        services.AddSingleton<TClient>();

        var sp = services.BuildServiceProvider();
        
        var client = sp.GetRequiredService<TClient>();
        
        var initialized = await client.TryInitializeAsync(TODO, Array.Empty<string>(), "en-US", false, CancellationToken.None);
        if (!initialized) throw new Exception("Failed to initialize client");

        return client;
    }

    protected ChatSessionData CreateChat(Character character)
    {
        var profile = new ProfileSettings
        {
            Name = "User"
        };
        var culture = CultureInfo.GetCultureInfoByIetfLanguageTag(character.Culture);
        var processor = new ChatTextProcessor(profile, character.Name, culture, new NullTokenCounter());
        var chat = new ChatSessionData
        {
            Culture = character.Culture,
            User = new ChatSessionDataUser
            {
                Name = profile.Name,
            },
            Chat = new Chat
            {
                Id = Guid.NewGuid(),
                CharacterId = Guid.NewGuid(),
            },
            Character = new ChatSessionDataCharacter
            {
                Name = character.Name,
                Description = processor.ProcessText(character.Description),
                Personality = processor.ProcessText(character.Personality),
                Scenario = processor.ProcessText(character.Scenario),
                FirstMessage = processor.ProcessText(character.FirstMessage),
            },
        };
        chat.AddMessage(chat.Character, chat.Character.FirstMessage);
        return chat;
    }
}