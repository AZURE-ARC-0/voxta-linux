using NUnit.Framework;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;
using Voxta.Characters.Samples;
using Voxta.IntegrationTests.Shared;
using Voxta.Services.Oobabooga;

namespace Voxta.IntegrationTests.Oobabooga;

public class OobaboogaIntegrationTests : IntegrationTestsBase
{
    [Test, Explicit]
    public async Task TestChat()
    {
        var chat = CreateChat(CatherineCharacter.Create());
        using (var token = chat.GetWriteToken())
        {
            token.AddMessage(chat.User, "I wanted to know more about you.");
            token.AddMessage(chat.Character, "I'm all yours!");
            token.AddMessage(chat.User, "What would be the perfect date for you?");
            token.AddMessage(chat.Character, "Oh, I think I'm blushing... that would be anything, as long as I'm with you!");
            token.AddMessage(chat.User, "You're cute, you know that?");
        }

        var client = await CreateClientAsync<OobaboogaTextGenService>(OobaboogaConstants.ServiceName);
        var reply = await client.GenerateReplyAsync(chat, CancellationToken.None);
        
        Console.WriteLine("### Prompt");
        Console.WriteLine(ServiceObserver.GetRecord(ServiceObserverKeys.TextGenPrompt)?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(reply);
    }

    [Test, Explicit]
    public async Task TestActionInference()
    {
        var chat = CreateChat(CatherineCharacter.Create());
        chat.Actions = new[] { "cry", "think", "leave", "smile", "frown" };
        using (var token = chat.GetWriteToken())
        {
            token.AddMessage(chat.User, "Tell me, how do you feel about this?");
            token.AddMessage(chat.Character, "This fills me with joy!");
        }

        var client = await CreateClientAsync<OobaboogaActionInferenceService>(OobaboogaConstants.ServiceName);
        var action = await client.SelectActionAsync(chat, CancellationToken.None);
        
        Console.WriteLine("### Prompt");
        Console.WriteLine(ServiceObserver.GetRecord(ServiceObserverKeys.ActionInferencePrompt)?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(action);
        
        Assert.That(action, Is.EqualTo("smile"));
    }

    [Test, Explicit]
    public async Task TestSummarization()
    {
        var chat = CreateChat(CatherineCharacter.Create());
        using (var token = chat.GetWriteToken())
        {
            token.AddMessage(chat.User, "I love apples, they taste delicious!");
            token.AddMessage(chat.Character, "Yeah? Personally, I hate them.");
            token.AddMessage(chat.User, "Ok, that's uncommon!");
            token.AddMessage(chat.Character, "I am uncommon, if you don't like me just log off!");
            token.AddMessage(chat.User, "I'm sorry, I didn't mean to offend you.");
            token.AddMessage(chat.Character, "It's ok, as long as you accept me for what I am.");
            token.AddMessage(chat.User, "So, what should we do then?");
            token.AddMessage(chat.Character, "We can talk about what we like, maybe?");
            token.AddMessage(chat.User, "Ok, well what do you like?");
            token.AddMessage(chat.Character, "I like you! ... I hope that's okay with you?");
        }
        var client = await CreateClientAsync<OobaboogaSummarizationService>(OobaboogaConstants.ServiceName);
        var summary = await client.SummarizeAsync(chat, chat.GetAllMessages(), CancellationToken.None);
        
        Console.WriteLine("### Prompt");
        Console.WriteLine(ServiceObserver.GetRecord(ServiceObserverKeys.SummarizationPrompt)?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(summary);
        
        StringAssert.Contains("apple", summary);
    }
}