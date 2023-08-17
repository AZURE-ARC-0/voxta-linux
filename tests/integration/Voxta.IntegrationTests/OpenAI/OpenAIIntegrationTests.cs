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
        using (var token = chat.GetWriteToken())
        {
            token.AddMessage(chat.User, "Tell me, how do you feel about this?");
        }

        var client = await CreateClientAsync<OpenAITextGenClient>(OpenAIConstants.ServiceName);
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
        chat.Actions = new[] { "cry", "think", "leave", "smile", "frown" };
        using (var token = chat.GetWriteToken())
        {
            token.AddMessage(chat.User, "Tell me, how do you feel about this?");
            token.AddMessage(chat.Character, "This fills me with joy!");
        }

        var client = await CreateClientAsync<OpenAIActionInferenceClient>(OpenAIConstants.ServiceName);
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
        using (var token = chat.GetWriteToken())
        {
            token.AddMessage(chat.User, "I have nothing to do right now, I thought we could chat?");
            token.AddMessage(chat.Character, "Yes! That would be nice, I was lonely. Tell me something about you!");
            token.AddMessage(chat.User, "I love apples, they taste delicious!");
            token.AddMessage(chat.Character, "Yeah? Personally, I hate them.");
            token.AddMessage(chat.User, "Really? That's uncommon!");
            token.AddMessage(chat.Character, "I guess I'm not your typical girl!");
        }

        var client = await CreateClientAsync<OpenAISummarizationService>(OpenAIConstants.ServiceName);
        var summary = await client.SummarizeAsync(chat, chat.GetAllMessages(), CancellationToken.None);
        
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