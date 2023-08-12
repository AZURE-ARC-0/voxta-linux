using System.Globalization;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;
using Voxta.Characters.Samples;
using Voxta.Core;
using Voxta.Data.LiteDB;
using Voxta.Security.Windows;

namespace Voxta.IntegrationTests.Shared;

public abstract class IntegrationTestsBase
{
    protected TestServiceObserver ServiceObserver { get; set; } = null!;

    [SetUp]
    public void Setup()
    {
    }

    protected async Task<TClient> CreateClientAsync<TClient>() where TClient : class, IService
    {
        var services = new ServiceCollection();
        using var httpClientFactory = new TestHttpClientFactory();
        services.AddSingleton<IHttpClientFactory>(httpClientFactory);
        
        var db = new LiteDatabase(@"../../../../../../../src/server/Voxta.Server/Data/Voxta.db");
        services.AddSingleton(db);
        
        var settingsRepository = new SettingsLiteDBRepository(db);
        services.AddSingleton<ISettingsRepository>(settingsRepository);
        
        var metrics = new TestPerformanceMetrics();
        services.AddSingleton<IPerformanceMetrics>(metrics);
        
        ServiceObserver = new TestServiceObserver();
        services.AddSingleton<IServiceObserver>(ServiceObserver);
        
        var encryptionProvider = new DPAPIEncryptionProvider();
        services.AddSingleton<ILocalEncryptionProvider>(encryptionProvider);

        services.AddSingleton<TClient>();

        var sp = services.BuildServiceProvider();
        
        var client = sp.GetRequiredService<TClient>();
        
        var initialized = await client.TryInitializeAsync(Array.Empty<string>(), "en-US", false, CancellationToken.None);
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
        var processor = new ChatTextProcessor(profile, character.Name);
        var chat = new ChatSessionData
        {
            UserName = "User",
            Chat = new Chat
            {
                Id = Guid.NewGuid(),
                CharacterId = Guid.NewGuid(),
            },
            Character = new CharacterCardExtended
            {
                Name = character.Name,
                Description = processor.ProcessText(character.Description, culture),
                Personality = processor.ProcessText(character.Personality, culture),
                Scenario = processor.ProcessText(character.Scenario, culture),
                FirstMessage = processor.ProcessText(character.FirstMessage, culture),
                Services = null!,
            },
        };
        chat.AddMessage(chat.Character.Name, chat.Character.FirstMessage);
        return chat;
    }
}