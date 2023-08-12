using NUnit.Framework;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;
using Voxta.Characters.Samples;
using Voxta.IntegrationTests.Shared;
using Voxta.Services.NovelAI;

namespace Voxta.IntegrationTests.NovelAI;

public class NovelAIIntegrationTests : IntegrationTestsBase
{
    [Test, Explicit]
    public async Task TestChat()
    {
        var chat = CreateChat(CatherineCharacter.Create());
        chat.AddMessage(chat.UserName, "Tell me, how do you feel about this?");

        var client = await CreateClientAsync<NovelAITextGenService>();
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

        var client = await CreateClientAsync<NovelAIActionInferenceService>();
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

        var client = await CreateClientAsync<NovelAISummarizationService>();
        var action = await client.SummarizeAsync(chat, CancellationToken.None);
        
        Console.WriteLine("### Prompt");
        Console.WriteLine(ServiceObserver.GetRecord(ServiceObserverKeys.SummarizationPrompt)?.Value);
        Console.WriteLine();
        Console.WriteLine("### Result");
        Console.WriteLine(action);
    }
}