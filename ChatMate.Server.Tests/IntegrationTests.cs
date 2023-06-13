using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace ChatMate.Server.Tests;

[TestFixture]
public class ServerIntegrationTest
{
    private static readonly HttpClient HttpClient = new();
    
    private Task _serverTask = null!;
    private CancellationTokenSource _serverCts = null!;
    private TcpClient _tcpClient = null!;
    private NetworkStream _tcpStream = null!;

    [SetUp]
    public void Setup()
    {
        _serverCts = new CancellationTokenSource();
        _serverTask = Program.Start(Array.Empty<string>(), _serverCts.Token);
        _tcpClient = new TcpClient();
        const int maxAttempts = 10;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                _tcpClient.Connect("localhost", 5384);
                _tcpStream = _tcpClient.GetStream();
                break; // Server has started, break the loop
            }
            catch (SocketException)
            {
                if (attempt == maxAttempts - 1)
                    throw;

                Task.Delay(100).Wait();
            }
        }
    }

    [TearDown]
    public void Teardown()
    {
        _tcpClient.Close();
        _tcpClient.Dispose();
        _serverCts.Cancel();
        _serverTask.Wait();
    }

    [Test]
    public async Task TestChatMessage()
    {
        await SendJson(new Message { Type = "Send", Content = "Hello World!" });

        var reply = await ReceiveJson();
        Assert.That(reply.Type, Is.EqualTo("Reply"));
        
        var speech = await ReceiveJson();
        Assert.That(reply.Type, Is.EqualTo("Speech"));

        var response = await HttpClient.GetAsync(speech.Content);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("audio/mpeg"));
            Assert.That(response.Content.Headers.ContentLength, Is.GreaterThan(1000));
        });
    }

    private async Task<Message> ReceiveJson()
    {
        var buffer = new byte[1024];
        var bytesRead = await _tcpStream.ReadAsync(buffer);
        var receivedMessage = JsonSerializer.Deserialize<Message>(buffer.AsMemory(0, bytesRead).Span);
        if (receivedMessage == null) throw new NullReferenceException("receivedMessage is null");
        return receivedMessage;
    }

    private async Task SendJson(Message message)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message);
        await _tcpStream.WriteAsync(json.AsMemory(), CancellationToken.None);
    }
}
