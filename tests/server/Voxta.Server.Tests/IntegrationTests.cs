using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Voxta.Abstractions.Model;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Voxta.Abstractions.Repositories;
using Voxta.Common;

namespace Voxta.Server.Tests;

[TestFixture]
public class WebSocketTest
{
    private readonly byte[] _buffer = new byte[1024 * 4];

    private WebSocketClient _wsClient = null!;
    private WebSocket _wsConnection = null!;
    private TestServer _server = null!;
    private HttpClient _httpClient = null!;

    [SetUp]
    public async Task SetUp()
    {
        var profileRepo = new Mock<IProfileRepository>();
        profileRepo.Setup(mock => mock.GetProfileAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ProfileSettings
        {
            Id = Guid.Empty.ToString(),
            Name = "User",
            TextGen = new ServicesList { Services = new[] { "Fakes" } },
            SpeechToText = new ServicesList { Services = new[] { "Fakes" } },
            TextToSpeech = new ServicesList { Services = new[] { "Fakes" } },
            ActionInference = new ServicesList { Services = new[] { "Fakes" } },
        });
            
        var webDir = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "server", "Voxta.Server"));
        var builder = WebHost.CreateDefaultBuilder()
            .UseStartup<Startup>()
            .UseContentRoot(webDir)
            .ConfigureTestServices(sc =>
            {
                sc.AddTransient<IProfileRepository>(_ => profileRepo.Object);
            });
        _server = new TestServer(builder);
        _httpClient = _server.CreateClient();
        _wsClient = _server.CreateWebSocketClient();
        var wsUri = new UriBuilder(_server.BaseAddress)
        {
            Scheme = "ws",
            Path = "/ws"
        }.Uri;
        _wsConnection = await _wsClient.ConnectAsync(wsUri, CancellationToken.None);
    }

    [TearDown]
    public async Task TearDown()
    {
        try
        {
            await _wsConnection.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
        catch (Exception)
        {
            // ignored
        }

        _server.Dispose();
    }

    [Test]
    public async Task SendMessageAndGetReply()
    {
        var welcome = await Receive<ServerWelcomeMessage>();
        Assert.That(welcome.Username, Is.EqualTo("User"));

        await Send(new ClientStartChatMessage
        {
            Character = new Character
            {
                Id = Guid.Empty,
                Name = "Bot",
                Description = "Desc",
                Personality = "Test",
                Scenario = "Test",
                FirstMessage = "First",
                Services = new CharacterServicesMap()
            }
        });
        
        var ready = await Receive<ServerReadyMessage>();
        Assert.That(ready.ChatId, Is.Not.EqualTo(Guid.Empty));

        await Send(new ClientSendMessage { Text = "Hello, world!", Actions = new[] { "action1, action2" } });

        var reply = await Receive<ServerReplyMessage>();
        Assert.That(reply.Text, Is.Not.Null.Or.Empty);
        
        var speech = await Receive<ServerSpeechMessage>();
        Assert.That(speech.Url, Does.Match(@"/tts/gens/.+\.wav"));
        
        var action = await Receive<ServerActionMessage>();
        Assert.That(action.Value, Is.Not.Null.Or.Empty);
        
        var response = await _httpClient.GetAsync(new Uri(_server.BaseAddress, speech.Url));
        if (!response.IsSuccessStatusCode)
            Assert.Fail($"GET {speech.Url}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), speech.Url);
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("audio/x-wav"), speech.Url);
            Assert.That(response.Content.Headers.ContentLength, Is.GreaterThan(1), speech.Url);
        });
    }
    
    private Task Send<T>(T message) where T : ClientMessage
    {
        var requestBytes = JsonSerializer.SerializeToUtf8Bytes<ClientMessage>(message, VoxtaJsonSerializer.SerializeOptions);
        return _wsConnection.SendAsync(
            requestBytes,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }

    private async Task<T> Receive<T>() where T : ServerMessage
    {
        var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        WebSocketReceiveResult result;
        try
        {
            result = await _wsConnection.ReceiveAsync(new ArraySegment<byte>(_buffer), timeout.Token);
        }
        catch (OperationCanceledException)
        {
            throw new Exception($"Did not receive a response while waiting for {typeof(T).Name}");
        }

        Assert.That(result.CloseStatus, Is.Null, result.CloseStatusDescription);
        ServerMessage? response;
        try
        {
            response = JsonSerializer.Deserialize<ServerMessage>(_buffer.AsMemory(0, result.Count).Span, VoxtaJsonSerializer.SerializeOptions);
        }
        catch (Exception e)
        {
            throw new JsonException($"Failed to deserialize message to type {typeof(T).Name}: {Encoding.UTF8.GetString(_buffer.AsMemory(0, result.Count).Span)}", e);
        }

        if (response == null) throw new NullReferenceException("Null response");
        if (response is not T typedResponse)
        {
            if (response is ServerErrorMessage errorMessage)
                throw new Exception("Server error: " + errorMessage.Message);

            throw new InvalidCastException($"Failed to cast response of type {response.GetType().Name} to type {typeof(T).Name}.");
        }
        return typedResponse;
    }
}
