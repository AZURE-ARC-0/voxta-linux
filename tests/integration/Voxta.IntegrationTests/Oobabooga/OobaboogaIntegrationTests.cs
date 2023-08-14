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
        chat.AddMessage(chat.User.Name, "I wanted to know more about you.");
        chat.AddMessage(chat.Character.Name, "I'm all yours!");
        chat.AddMessage(chat.User.Name, "What would be the perfect date for you?");
        chat.AddMessage(chat.Character.Name, "Oh, I think I'm blushing... that would be anything, as long as I'm with you!");
        chat.AddMessage(chat.User.Name, "You're cute, you know that?");

        var client = await CreateClientAsync<OobaboogaTextGenService>();
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
        chat.AddMessage(chat.User.Name, "Tell me, how do you feel about this?");
        chat.AddMessage(chat.Character.Name, "This fills me with joy!");
        chat.Actions = new[] { "cry", "think", "leave", "smile", "frown" };

        var client = await CreateClientAsync<OobaboogaActionInferenceService>();
        var action = await client.SelectActionAsync(chat, CancellationToken.None);
        
        Console.WriteLine("### Prompt");
        Console.WriteLine(ServiceObserver.GetRecord(ServiceObserverKeys.ActionInferencePrompt)?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(action);
    }
    
    [Test, Explicit]
    public async Task TestSummarization()
    {
        var chat = CreateChat(CatherineCharacter.Create());
        chat.AddMessage(chat.User.Name, "I love apples, they taste delicious!");
        chat.AddMessage(chat.Character.Name, "Yeah? Personally, I hate them.");
        chat.AddMessage(chat.User.Name, "Ok, that's uncommon!");
        chat.AddMessage(chat.Character.Name, "I am uncommon, if you don't like me just log off!");
        chat.AddMessage(chat.User.Name, "I'm sorry, I didn't mean to offend you.");
        chat.AddMessage(chat.Character.Name, "It's ok, as long as you accept me for what I am.");
        chat.AddMessage(chat.User.Name, "So, what should we do then?");
        chat.AddMessage(chat.Character.Name, "We can talk about what we like, maybe?");
        chat.AddMessage(chat.User.Name, "Ok, well what do you like?");
        chat.AddMessage(chat.Character.Name, "I like you! ... I hope that's okay with you?");

        var client = await CreateClientAsync<OobaboogaSummarizationService>();
        var action = await client.SummarizeAsync(chat, chat.Messages, CancellationToken.None);
        
        Console.WriteLine("### Prompt");
        Console.WriteLine(ServiceObserver.GetRecord(ServiceObserverKeys.SummarizationPrompt)?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(action);
    }
}