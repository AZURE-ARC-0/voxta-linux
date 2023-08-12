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
        chat.AddMessage(chat.UserName, "oh wow, I did not expect you to be so...");
        chat.AddMessage(chat.Character.Name, "So... what? You'll make me blush!");
        chat.AddMessage(chat.UserName, "so beautiful, I am sorry I am just a bit nervous");
        chat.AddMessage(chat.Character.Name, "No worries, I'm nervous too! I'm just glad you're here. So, what do you want to talk about?");
        chat.AddMessage(chat.UserName, "well it's the first time I talk with a computer, I am not sure what to say");

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
        chat.AddMessage(chat.UserName, "Tell me, how do you feel about this?");
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
        chat.AddMessage(chat.UserName, "I love apples, they taste delicious!");
        chat.AddMessage(chat.Character.Name, "Yeah? Personally, I hate them.");
        chat.AddMessage(chat.UserName, "Ok, that's uncommon!");
        chat.AddMessage(chat.Character.Name, "I am uncommon, if you don't like me just log off!");
        chat.AddMessage(chat.UserName, "I'm sorry, I didn't mean to offend you.");
        chat.AddMessage(chat.Character.Name, "It's ok, as long as you accept me for what I am.");
        chat.AddMessage(chat.UserName, "So, what should we do then?");
        chat.AddMessage(chat.Character.Name, "We can talk about what we like, maybe?");
        chat.AddMessage(chat.UserName, "Ok, well what do you like?");
        chat.AddMessage(chat.Character.Name, "I like you! ... I hope that's okay with you?");

        var client = await CreateClientAsync<OobaboogaSummarizationService>();
        var action = await client.SummarizeAsync(chat, CancellationToken.None);
        
        Console.WriteLine("### Prompt");
        Console.WriteLine(ServiceObserver.GetRecord(ServiceObserverKeys.SummarizationPrompt)?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(action);
    }
}