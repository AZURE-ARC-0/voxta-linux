using NUnit.Framework;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;
using Voxta.Characters.Samples;
using Voxta.IntegrationTests.Shared;
using Voxta.Services.OpenAI;

namespace Voxta.IntegrationTests.OpenAI;

public class OpenAIIntegrationTests : IntegrationTestsBase
{
    [Test, Explicit]
    public async Task TestChat()
    {
        var chat = CreateChat(CatherineCharacter.Create());
        chat.AddMessage(chat.User, "Tell me, how do you feel about this?");

        var client = await CreateClientAsync<OpenAITextGenClient>();
        var reply = await client.GenerateReplyAsync(chat, CancellationToken.None);
        
        Console.WriteLine("### Prompt (System)");
        Console.WriteLine(ServiceObserver.GetRecord($"{ServiceObserverKeys.TextGenPrompt}[System]")?.Value);
        Console.WriteLine("### Prompt (User)");
        Console.WriteLine(ServiceObserver.GetRecord($"{ServiceObserverKeys.TextGenPrompt}[User]")?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(reply);
    }
    
    [Test, Explicit]
    public async Task TestActionInference()
    {
        var chat = CreateChat(CatherineCharacter.Create());
        chat.AddMessage(chat.User, "Tell me, how do you feel about this?");
        chat.AddMessage(chat.Character, "This fills me with joy!");
        chat.Actions = new[] { "cry", "think", "leave", "smile", "frown" };

        var client = await CreateClientAsync<OpenAIActionInferenceClient>();
        var action = await client.SelectActionAsync(chat, CancellationToken.None);
        
        Console.WriteLine("### Prompt (System)");
        Console.WriteLine(ServiceObserver.GetRecord($"{ServiceObserverKeys.ActionInferencePrompt}[System]")?.Value);
        Console.WriteLine("### Prompt (User)");
        Console.WriteLine(ServiceObserver.GetRecord($"{ServiceObserverKeys.ActionInferencePrompt}[User]")?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(action);
        
        Assert.That(action, Is.EqualTo("smile"));
    }
    
    [Test, Explicit]
    public async Task TestSummarization()
    {
        var chat = CreateChat(CatherineCharacter.Create());
        chat.AddMessage(chat.User, "I have nothing to do right now, I thought we could chat?");
        chat.AddMessage(chat.Character, "Yes! That would be nice, I was lonely. Tell me something about you!");
        chat.AddMessage(chat.User, "I love apples, they taste delicious!");
        chat.AddMessage(chat.Character, "Yeah? Personally, I hate them.");
        chat.AddMessage(chat.User, "Really? That's uncommon!");
        chat.AddMessage(chat.Character, "I guess I'm not your typical girl!");

        var client = await CreateClientAsync<OpenAISummarizationService>();
        var summary = await client.SummarizeAsync(chat, chat.Messages, CancellationToken.None);
        
        Console.WriteLine("### Prompt (System)");
        Console.WriteLine(ServiceObserver.GetRecord($"{ServiceObserverKeys.SummarizationPrompt}[System]")?.Value);
        Console.WriteLine("### Prompt (User)");
        Console.WriteLine(ServiceObserver.GetRecord($"{ServiceObserverKeys.SummarizationPrompt}[User]")?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(summary);

        StringAssert.Contains("apple", summary);
    }
}